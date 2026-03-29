using System.Data;

using ArchiForge.ArtifactSynthesis.Interfaces;
using ArchiForge.ArtifactSynthesis.Models;
using ArchiForge.Core.Scoping;
using ArchiForge.Persistence.Connections;
using ArchiForge.Persistence.Serialization;

using Dapper;

using Microsoft.Data.SqlClient;

namespace ArchiForge.Persistence.Repositories;

/// <summary>
/// SQL Server-backed <see cref="IArtifactBundleRepository"/> with dual-write to legacy JSON columns and
/// relational tables for artifacts (content as plain NVARCHAR(MAX)), metadata, artifact–decision links,
/// and trace lists. Reads prefer relational slices when rows exist; trace scalars (TraceId, etc.) remain
/// sourced from <c>TraceJson</c> when present.
/// </summary>
public sealed class SqlArtifactBundleRepository(ISqlConnectionFactory connectionFactory) : IArtifactBundleRepository
{
    public async Task SaveAsync(
        ArtifactBundle bundle,
        CancellationToken ct,
        IDbConnection? connection = null,
        IDbTransaction? transaction = null)
    {
        ArgumentNullException.ThrowIfNull(bundle);

        if (connection is not null)
        {
            await SaveCoreAsync(bundle, connection, transaction, ct);
            return;
        }

        await using SqlConnection owned = await connectionFactory.CreateOpenConnectionAsync(ct);
        using SqlTransaction tx = owned.BeginTransaction();

        try
        {
            await SaveCoreAsync(bundle, owned, tx, ct);
            tx.Commit();
        }
        catch
        {
            tx.Rollback();
            throw;
        }
    }

    private static async Task SaveCoreAsync(
        ArtifactBundle bundle,
        IDbConnection connection,
        IDbTransaction? transaction,
        CancellationToken ct)
    {
        const string sql = """
            INSERT INTO dbo.ArtifactBundles
            (
                BundleId, RunId, ManifestId, CreatedUtc, ArtifactsJson, TraceJson,
                TenantId, WorkspaceId, ProjectId
            )
            VALUES
            (
                @BundleId, @RunId, @ManifestId, @CreatedUtc, @ArtifactsJson, @TraceJson,
                @TenantId, @WorkspaceId, @ProjectId
            );
            """;

        string artifactsJson = JsonEntitySerializer.Serialize(bundle.Artifacts);
        string traceJson = JsonEntitySerializer.Serialize(bundle.Trace);

        object args = new
        {
            bundle.BundleId,
            bundle.RunId,
            bundle.ManifestId,
            bundle.CreatedUtc,
            ArtifactsJson = artifactsJson,
            TraceJson = traceJson,
            bundle.TenantId,
            bundle.WorkspaceId,
            bundle.ProjectId,
        };

        await connection.ExecuteAsync(new CommandDefinition(sql, args, transaction, cancellationToken: ct));

        await InsertArtifactBundleArtifactsRelationalAsync(bundle, connection, transaction, ct);
        await InsertArtifactBundleTraceRelationalAsync(bundle, connection, transaction, ct);
    }

    private static async Task InsertArtifactBundleArtifactsRelationalAsync(
        ArtifactBundle bundle,
        IDbConnection connection,
        IDbTransaction? transaction,
        CancellationToken ct)
    {
        Guid bundleId = bundle.BundleId;

        const string insertArtifactSql = """
            INSERT INTO dbo.ArtifactBundleArtifacts
            (
                BundleId, SortOrder, ArtifactId, RunId, ManifestId, CreatedUtc,
                ArtifactType, Name, Format, Content, ContentHash
            )
            VALUES
            (
                @BundleId, @SortOrder, @ArtifactId, @RunId, @ManifestId, @CreatedUtc,
                @ArtifactType, @Name, @Format, @Content, @ContentHash
            );
            """;

        for (int i = 0; i < bundle.Artifacts.Count; i++)
        {
            SynthesizedArtifact a = bundle.Artifacts[i];

            await connection.ExecuteAsync(
                new CommandDefinition(
                    insertArtifactSql,
                    new
                    {
                        BundleId = bundleId,
                        SortOrder = i,
                        a.ArtifactId,
                        a.RunId,
                        a.ManifestId,
                        a.CreatedUtc,
                        a.ArtifactType,
                        a.Name,
                        a.Format,
                        Content = a.Content ?? string.Empty,
                        a.ContentHash,
                    },
                    transaction,
                    cancellationToken: ct));

            int metaOrder = 0;

            foreach (KeyValuePair<string, string> meta in a.Metadata)
            {
                const string insertMetaSql = """
                    INSERT INTO dbo.ArtifactBundleArtifactMetadata
                    (BundleId, ArtifactSortOrder, MetaSortOrder, MetaKey, MetaValue)
                    VALUES (@BundleId, @ArtifactSortOrder, @MetaSortOrder, @MetaKey, @MetaValue);
                    """;

                await connection.ExecuteAsync(
                    new CommandDefinition(
                        insertMetaSql,
                        new
                        {
                            BundleId = bundleId,
                            ArtifactSortOrder = i,
                            MetaSortOrder = metaOrder,
                            MetaKey = meta.Key,
                            MetaValue = meta.Value ?? string.Empty,
                        },
                        transaction,
                        cancellationToken: ct));

                metaOrder++;
            }

            for (int d = 0; d < a.ContributingDecisionIds.Count; d++)
            {
                const string insertDecSql = """
                    INSERT INTO dbo.ArtifactBundleArtifactDecisionLinks
                    (BundleId, ArtifactSortOrder, LinkSortOrder, DecisionId)
                    VALUES (@BundleId, @ArtifactSortOrder, @LinkSortOrder, @DecisionId);
                    """;

                await connection.ExecuteAsync(
                    new CommandDefinition(
                        insertDecSql,
                        new
                        {
                            BundleId = bundleId,
                            ArtifactSortOrder = i,
                            LinkSortOrder = d,
                            DecisionId = a.ContributingDecisionIds[d],
                        },
                        transaction,
                        cancellationToken: ct));
            }
        }
    }

    private static async Task InsertArtifactBundleTraceRelationalAsync(
        ArtifactBundle bundle,
        IDbConnection connection,
        IDbTransaction? transaction,
        CancellationToken ct)
    {
        await InsertArtifactBundleTraceGeneratorsRelationalAsync(bundle, connection, transaction, ct);
        await InsertArtifactBundleTraceDecisionLinksRelationalAsync(bundle, connection, transaction, ct);
        await InsertArtifactBundleTraceNotesRelationalAsync(bundle, connection, transaction, ct);
    }

    private static async Task InsertArtifactBundleTraceGeneratorsRelationalAsync(
        ArtifactBundle bundle,
        IDbConnection connection,
        IDbTransaction? transaction,
        CancellationToken ct)
    {
        Guid bundleId = bundle.BundleId;
        SynthesisTrace trace = bundle.Trace;

        for (int g = 0; g < trace.GeneratorsUsed.Count; g++)
        {
            const string insertGenSql = """
                INSERT INTO dbo.ArtifactBundleTraceGenerators (BundleId, SortOrder, GeneratorName)
                VALUES (@BundleId, @SortOrder, @GeneratorName);
                """;

            await connection.ExecuteAsync(
                new CommandDefinition(
                    insertGenSql,
                    new
                    {
                        BundleId = bundleId,
                        SortOrder = g,
                        GeneratorName = trace.GeneratorsUsed[g],
                    },
                    transaction,
                    cancellationToken: ct));
        }
    }

    private static async Task InsertArtifactBundleTraceDecisionLinksRelationalAsync(
        ArtifactBundle bundle,
        IDbConnection connection,
        IDbTransaction? transaction,
        CancellationToken ct)
    {
        Guid bundleId = bundle.BundleId;
        SynthesisTrace trace = bundle.Trace;

        for (int s = 0; s < trace.SourceDecisionIds.Count; s++)
        {
            const string insertTraceDecSql = """
                INSERT INTO dbo.ArtifactBundleTraceDecisionLinks (BundleId, SortOrder, DecisionId)
                VALUES (@BundleId, @SortOrder, @DecisionId);
                """;

            await connection.ExecuteAsync(
                new CommandDefinition(
                    insertTraceDecSql,
                    new
                    {
                        BundleId = bundleId,
                        SortOrder = s,
                        DecisionId = trace.SourceDecisionIds[s],
                    },
                    transaction,
                    cancellationToken: ct));
        }
    }

    private static async Task InsertArtifactBundleTraceNotesRelationalAsync(
        ArtifactBundle bundle,
        IDbConnection connection,
        IDbTransaction? transaction,
        CancellationToken ct)
    {
        Guid bundleId = bundle.BundleId;
        SynthesisTrace trace = bundle.Trace;

        for (int n = 0; n < trace.Notes.Count; n++)
        {
            const string insertNoteSql = """
                INSERT INTO dbo.ArtifactBundleTraceNotes (BundleId, SortOrder, NoteText)
                VALUES (@BundleId, @SortOrder, @NoteText);
                """;

            await connection.ExecuteAsync(
                new CommandDefinition(
                    insertNoteSql,
                    new
                    {
                        BundleId = bundleId,
                        SortOrder = n,
                        NoteText = trace.Notes[n] ?? string.Empty,
                    },
                    transaction,
                    cancellationToken: ct));
        }
    }

    /// <summary>
    /// Loads a bundle by primary key (admin/backfill scenarios).
    /// </summary>
    public async Task<ArtifactBundle?> GetByBundleIdAsync(Guid bundleId, CancellationToken ct)
    {
        await using SqlConnection connection = await connectionFactory.CreateOpenConnectionAsync(ct);
        return await GetByBundleIdAsync(bundleId, connection, null, ct);
    }

    /// <inheritdoc cref="GetByBundleIdAsync(System.Guid,System.Threading.CancellationToken)"/>
    public async Task<ArtifactBundle?> GetByBundleIdAsync(
        Guid bundleId,
        IDbConnection connection,
        IDbTransaction? transaction,
        CancellationToken ct)
    {
        const string sql = """
            SELECT
                TenantId, WorkspaceId, ProjectId,
                BundleId, RunId, ManifestId, CreatedUtc, ArtifactsJson, TraceJson
            FROM dbo.ArtifactBundles
            WHERE BundleId = @BundleId;
            """;

        ArtifactBundleRow? row = await connection.QuerySingleOrDefaultAsync<ArtifactBundleRow>(
            new CommandDefinition(
                sql,
                new
                {
                    BundleId = bundleId,
                },
                transaction,
                cancellationToken: ct));

        if (row is null)
            return null;

        SqlConnection sqlConnection = connection as SqlConnection
            ?? throw new InvalidOperationException("SQL Server backfill requires SqlConnection.");

        return await HydrateBundleAsync(sqlConnection, row, ct);
    }

    public async Task<ArtifactBundle?> GetByManifestIdAsync(ScopeContext scope, Guid manifestId, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(scope);

        const string sql = """
            SELECT TOP 1
                TenantId, WorkspaceId, ProjectId,
                BundleId, RunId, ManifestId, CreatedUtc, ArtifactsJson, TraceJson
            FROM dbo.ArtifactBundles
            WHERE TenantId = @TenantId
              AND WorkspaceId = @WorkspaceId
              AND ProjectId = @ScopeProjectId
              AND ManifestId = @ManifestId
            ORDER BY CreatedUtc DESC;
            """;

        await using SqlConnection connection = await connectionFactory.CreateOpenConnectionAsync(ct);
        ArtifactBundleRow? row = await connection.QuerySingleOrDefaultAsync<ArtifactBundleRow>(
            new CommandDefinition(
                sql,
                new
                {
                    scope.TenantId,
                    scope.WorkspaceId,
                    ScopeProjectId = scope.ProjectId,
                    ManifestId = manifestId,
                },
                cancellationToken: ct));

        if (row is null)
            return null;

        try
        {
            return await HydrateBundleAsync(connection, row, ct);
        }
        catch (InvalidOperationException ex)
        {
            throw new InvalidOperationException(
                $"Failed to deserialize ArtifactBundle '{row.BundleId}' for manifest '{row.ManifestId}'. " +
                "The stored JSON may be corrupt or from an incompatible schema version.", ex);
        }
    }

    private static async Task<ArtifactBundle> HydrateBundleAsync(
        SqlConnection connection,
        ArtifactBundleRow row,
        CancellationToken ct)
    {
        Guid bundleId = row.BundleId;

        int artifactCount = await ScalarCountAsync(
            connection,
            "SELECT COUNT(1) FROM dbo.ArtifactBundleArtifacts WHERE BundleId = @BundleId",
            new
            {
                BundleId = bundleId,
            },
            ct);

        int genCount = await ScalarCountAsync(
            connection,
            "SELECT COUNT(1) FROM dbo.ArtifactBundleTraceGenerators WHERE BundleId = @BundleId",
            new
            {
                BundleId = bundleId,
            },
            ct);

        int traceDecCount = await ScalarCountAsync(
            connection,
            "SELECT COUNT(1) FROM dbo.ArtifactBundleTraceDecisionLinks WHERE BundleId = @BundleId",
            new
            {
                BundleId = bundleId,
            },
            ct);

        int notesCount = await ScalarCountAsync(
            connection,
            "SELECT COUNT(1) FROM dbo.ArtifactBundleTraceNotes WHERE BundleId = @BundleId",
            new
            {
                BundleId = bundleId,
            },
            ct);

        List<SynthesizedArtifact> artifacts = artifactCount > 0
            ? await LoadArtifactsRelationalAsync(connection, bundleId, ct)
            : JsonEntitySerializer.Deserialize<List<SynthesizedArtifact>>(row.ArtifactsJson);

        SynthesisTrace trace = JsonEntitySerializer.Deserialize<SynthesisTrace>(row.TraceJson);

        if (genCount > 0)
        {
            trace.GeneratorsUsed = await LoadOrderedStringsAsync(
                connection,
                """
                SELECT GeneratorName AS Item
                FROM dbo.ArtifactBundleTraceGenerators
                WHERE BundleId = @BundleId
                ORDER BY SortOrder;
                """,
                bundleId,
                ct);
        }

        if (traceDecCount > 0)
        {
            trace.SourceDecisionIds = await LoadOrderedStringsAsync(
                connection,
                """
                SELECT DecisionId AS Item
                FROM dbo.ArtifactBundleTraceDecisionLinks
                WHERE BundleId = @BundleId
                ORDER BY SortOrder;
                """,
                bundleId,
                ct);
        }

        if (notesCount > 0)
        {
            trace.Notes = await LoadOrderedStringsAsync(
                connection,
                """
                SELECT NoteText AS Item
                FROM dbo.ArtifactBundleTraceNotes
                WHERE BundleId = @BundleId
                ORDER BY SortOrder;
                """,
                bundleId,
                ct);
        }

        return new ArtifactBundle
        {
            TenantId = row.TenantId,
            WorkspaceId = row.WorkspaceId,
            ProjectId = row.ProjectId,
            BundleId = row.BundleId,
            RunId = row.RunId,
            ManifestId = row.ManifestId,
            CreatedUtc = row.CreatedUtc,
            Artifacts = artifacts,
            Trace = trace,
        };
    }

    private static async Task<List<SynthesizedArtifact>> LoadArtifactsRelationalAsync(
        SqlConnection connection,
        Guid bundleId,
        CancellationToken ct)
    {
        const string artifactsSql = """
            SELECT SortOrder, ArtifactId, RunId, ManifestId, CreatedUtc,
                   ArtifactType, Name, Format, Content, ContentHash
            FROM dbo.ArtifactBundleArtifacts
            WHERE BundleId = @BundleId
            ORDER BY SortOrder;
            """;

        List<ArtifactSliceRow> artifactRows = (await connection.QueryAsync<ArtifactSliceRow>(
            new CommandDefinition(
                artifactsSql,
                new
                {
                    BundleId = bundleId,
                },
                cancellationToken: ct))).ToList();

        if (artifactRows.Count == 0)
            return [];

        const string metaSql = """
            SELECT ArtifactSortOrder, MetaSortOrder, MetaKey, MetaValue
            FROM dbo.ArtifactBundleArtifactMetadata
            WHERE BundleId = @BundleId
            ORDER BY ArtifactSortOrder, MetaSortOrder;
            """;

        List<MetadataSliceRow> metaRows = (await connection.QueryAsync<MetadataSliceRow>(
            new CommandDefinition(
                metaSql,
                new
                {
                    BundleId = bundleId,
                },
                cancellationToken: ct))).ToList();

        const string decSql = """
            SELECT ArtifactSortOrder, LinkSortOrder, DecisionId
            FROM dbo.ArtifactBundleArtifactDecisionLinks
            WHERE BundleId = @BundleId
            ORDER BY ArtifactSortOrder, LinkSortOrder;
            """;

        List<ArtifactDecisionSliceRow> decisionRows = (await connection.QueryAsync<ArtifactDecisionSliceRow>(
            new CommandDefinition(
                decSql,
                new
                {
                    BundleId = bundleId,
                },
                cancellationToken: ct))).ToList();

        Dictionary<int, Dictionary<string, string>> metaByArtifact = new();

        foreach (MetadataSliceRow mr in metaRows)
        {
            if (!metaByArtifact.TryGetValue(mr.ArtifactSortOrder, out Dictionary<string, string>? dict))
            {
                dict = new Dictionary<string, string>(StringComparer.Ordinal);
                metaByArtifact[mr.ArtifactSortOrder] = dict;
            }

            dict[mr.MetaKey] = mr.MetaValue;
        }

        Dictionary<int, List<string>> decisionsByArtifact = new();

        foreach (ArtifactDecisionSliceRow dr in decisionRows)
        {
            if (!decisionsByArtifact.TryGetValue(dr.ArtifactSortOrder, out List<string>? list))
            {
                list = [];
                decisionsByArtifact[dr.ArtifactSortOrder] = list;
            }

            list.Add(dr.DecisionId);
        }

        List<SynthesizedArtifact> result = [];

        foreach (ArtifactSliceRow ar in artifactRows)
        {
            metaByArtifact.TryGetValue(ar.SortOrder, out Dictionary<string, string>? meta);
            meta ??= new Dictionary<string, string>(StringComparer.Ordinal);

            decisionsByArtifact.TryGetValue(ar.SortOrder, out List<string>? decIds);
            decIds ??= [];

            result.Add(
                new SynthesizedArtifact
                {
                    ArtifactId = ar.ArtifactId,
                    RunId = ar.RunId,
                    ManifestId = ar.ManifestId,
                    CreatedUtc = ar.CreatedUtc,
                    ArtifactType = ar.ArtifactType,
                    Name = ar.Name,
                    Format = ar.Format,
                    Content = ar.Content ?? string.Empty,
                    ContentHash = ar.ContentHash,
                    Metadata = meta,
                    ContributingDecisionIds = decIds,
                });
        }

        return result;
    }

    private static async Task<List<string>> LoadOrderedStringsAsync(
        SqlConnection connection,
        string sql,
        Guid bundleId,
        CancellationToken ct)
    {
        IEnumerable<string> rows = await connection.QueryAsync<string>(
            new CommandDefinition(
                sql,
                new
                {
                    BundleId = bundleId,
                },
                cancellationToken: ct));

        return rows.ToList();
    }

    private static async Task<int> ScalarCountAsync(
        IDbConnection connection,
        IDbTransaction? transaction,
        string sql,
        object param,
        CancellationToken ct)
    {
        int count = await connection.ExecuteScalarAsync<int>(new CommandDefinition(sql, param, transaction, cancellationToken: ct));
        return count;
    }

    private static async Task<int> ScalarCountAsync(
        SqlConnection connection,
        string sql,
        object param,
        CancellationToken ct)
    {
        int count = await connection.ExecuteScalarAsync<int>(new CommandDefinition(sql, param, cancellationToken: ct));
        return count;
    }

    /// <summary>
    /// Inserts relational artifact/trace slices that are still empty while JSON columns contain data (idempotent per slice).
    /// </summary>
    internal static async Task BackfillRelationalSlicesAsync(
        ArtifactBundle bundle,
        IDbConnection connection,
        IDbTransaction? transaction,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(bundle);
        ArgumentNullException.ThrowIfNull(connection);

        Guid bundleId = bundle.BundleId;

        int artifactRowCount = await ScalarCountAsync(
            connection,
            transaction,
            "SELECT COUNT(1) FROM dbo.ArtifactBundleArtifacts WHERE BundleId = @BundleId",
            new
            {
                BundleId = bundleId,
            },
            ct);

        int genCount = await ScalarCountAsync(
            connection,
            transaction,
            "SELECT COUNT(1) FROM dbo.ArtifactBundleTraceGenerators WHERE BundleId = @BundleId",
            new
            {
                BundleId = bundleId,
            },
            ct);

        int traceDecCount = await ScalarCountAsync(
            connection,
            transaction,
            "SELECT COUNT(1) FROM dbo.ArtifactBundleTraceDecisionLinks WHERE BundleId = @BundleId",
            new
            {
                BundleId = bundleId,
            },
            ct);

        int notesCount = await ScalarCountAsync(
            connection,
            transaction,
            "SELECT COUNT(1) FROM dbo.ArtifactBundleTraceNotes WHERE BundleId = @BundleId",
            new
            {
                BundleId = bundleId,
            },
            ct);

        if (artifactRowCount == 0 && bundle.Artifacts.Count > 0)
        {
            await InsertArtifactBundleArtifactsRelationalAsync(bundle, connection, transaction, ct);
            await InsertArtifactBundleTraceRelationalAsync(bundle, connection, transaction, ct);
            return;
        }

        if (genCount == 0 && bundle.Trace.GeneratorsUsed.Count > 0)
            await InsertArtifactBundleTraceGeneratorsRelationalAsync(bundle, connection, transaction, ct);

        if (traceDecCount == 0 && bundle.Trace.SourceDecisionIds.Count > 0)
            await InsertArtifactBundleTraceDecisionLinksRelationalAsync(bundle, connection, transaction, ct);

        if (notesCount == 0 && bundle.Trace.Notes.Count > 0)
            await InsertArtifactBundleTraceNotesRelationalAsync(bundle, connection, transaction, ct);
    }

    private sealed class ArtifactBundleRow
    {
        public Guid TenantId
        {
            get; init;
        }

        public Guid WorkspaceId
        {
            get; init;
        }

        public Guid ProjectId
        {
            get; init;
        }

        public Guid BundleId
        {
            get; init;
        }

        public Guid RunId
        {
            get; init;
        }

        public Guid ManifestId
        {
            get; init;
        }

        public DateTime CreatedUtc
        {
            get; init;
        }

        public string ArtifactsJson { get; init; } = null!;

        public string TraceJson { get; init; } = null!;
    }

    private sealed class ArtifactSliceRow
    {
        public int SortOrder
        {
            get; init;
        }

        public Guid ArtifactId
        {
            get; init;
        }

        public Guid RunId
        {
            get; init;
        }

        public Guid ManifestId
        {
            get; init;
        }

        public DateTime CreatedUtc
        {
            get; init;
        }

        public string ArtifactType { get; init; } = null!;

        public string Name { get; init; } = null!;

        public string Format { get; init; } = null!;

        public string? Content
        {
            get; init;
        }

        public string ContentHash { get; init; } = null!;
    }

    private sealed class MetadataSliceRow
    {
        public int ArtifactSortOrder
        {
            get; init;
        }

        public int MetaSortOrder
        {
            get; init;
        }

        public string MetaKey { get; init; } = null!;

        public string MetaValue { get; init; } = null!;
    }

    private sealed class ArtifactDecisionSliceRow
    {
        public int ArtifactSortOrder
        {
            get; init;
        }

        public int LinkSortOrder
        {
            get; init;
        }

        public string DecisionId { get; init; } = null!;
    }
}
