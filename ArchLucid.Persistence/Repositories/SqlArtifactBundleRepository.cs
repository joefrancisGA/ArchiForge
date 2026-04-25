using System.Data;
using System.Diagnostics.CodeAnalysis;

using ArchLucid.ArtifactSynthesis.Interfaces;
using ArchLucid.ArtifactSynthesis.Models;
using ArchLucid.Core.Scoping;
using ArchLucid.Persistence.ArtifactBundles;
using ArchLucid.Persistence.BlobStore;
using ArchLucid.Persistence.Connections;
using ArchLucid.Persistence.RelationalRead;
using ArchLucid.Persistence.Serialization;

using Dapper;

using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace ArchLucid.Persistence.Repositories;

/// <summary>
///     SQL Server-backed <see cref="IArtifactBundleRepository" /> with dual-write to legacy JSON columns and
///     relational tables for artifacts (content as plain NVARCHAR(MAX)), metadata, artifact–decision links,
///     and trace lists. Reads prefer relational slices when rows exist; trace scalars (TraceId, etc.) remain
///     sourced from <c>TraceJson</c> when present.
/// </summary>
[ExcludeFromCodeCoverage(Justification = "SQL-dependent repository; requires live SQL Server for integration testing.")]
public sealed class SqlArtifactBundleRepository(
    ISqlConnectionFactory connectionFactory,
    IArtifactBlobStore blobStore,
    IOptionsMonitor<ArtifactLargePayloadOptions> largePayloadOptions) : IArtifactBundleRepository
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
        await using SqlTransaction tx = owned.BeginTransaction();

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

    public async Task<ArtifactBundle?> GetByManifestIdAsync(ScopeContext scope, Guid manifestId, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(scope);

        const string sql = """
                           SELECT TOP 1
                               TenantId, WorkspaceId, ProjectId,
                               BundleId, RunId, ManifestId, CreatedUtc, ArtifactsJson, TraceJson, BundlePayloadBlobUri
                           FROM dbo.ArtifactBundles
                           WHERE TenantId = @TenantId
                             AND WorkspaceId = @WorkspaceId
                             AND ProjectId = @ScopeProjectId
                             AND ManifestId = @ManifestId
                           ORDER BY CreatedUtc DESC;
                           """;

        await using SqlConnection connection = await connectionFactory.CreateOpenConnectionAsync(ct);
        ArtifactBundleStorageRow? row = await connection.QuerySingleOrDefaultAsync<ArtifactBundleStorageRow>(
            new CommandDefinition(
                sql,
                new { scope.TenantId, scope.WorkspaceId, ScopeProjectId = scope.ProjectId, ManifestId = manifestId },
                cancellationToken: ct));

        if (row is null)
            return null;

        row = await ApplyBundleBlobOverlayIfPresentAsync(row, ct);

        try
        {
            return await ArtifactBundleRelationalRead.HydrateBundleAsync(connection, row, blobStore, ct);
        }
        catch (InvalidOperationException ex)
        {
            throw new InvalidOperationException(
                $"Failed to deserialize ArtifactBundle '{row.BundleId}' for manifest '{row.ManifestId}'. " +
                "The stored JSON may be corrupt or from an incompatible schema version.", ex);
        }
    }

    private async Task SaveCoreAsync(
        ArtifactBundle bundle,
        IDbConnection connection,
        IDbTransaction? transaction,
        CancellationToken ct)
    {
        const string sql = """
                           INSERT INTO dbo.ArtifactBundles
                           (
                               BundleId, RunId, ManifestId, CreatedUtc, ArtifactsJson, TraceJson,
                               TenantId, WorkspaceId, ProjectId, BundlePayloadBlobUri
                           )
                           VALUES
                           (
                               @BundleId, @RunId, @ManifestId, @CreatedUtc, @ArtifactsJson, @TraceJson,
                               @TenantId, @WorkspaceId, @ProjectId, @BundlePayloadBlobUri
                           );
                           """;

        string artifactsJson = JsonEntitySerializer.Serialize(bundle.Artifacts);
        string traceJson = JsonEntitySerializer.Serialize(bundle.Trace);

        ArtifactLargePayloadOptions payloadOpts = largePayloadOptions.CurrentValue;
        string? bundlePayloadBlobUri = null;

        if (LargePayloadOffloadEvaluator.ShouldOffloadManifestOrBundle(
                payloadOpts,
                ArtifactBundlePayloadBlobEnvelope.SumUtf16Length(artifactsJson, traceJson)))
        {
            ArtifactBundlePayloadBlobEnvelope envelope =
                ArtifactBundlePayloadBlobEnvelope.FromJsonPair(artifactsJson, traceJson);
            bundlePayloadBlobUri = await blobStore.WriteAsync(
                "artifact-bundles",
                $"{bundle.BundleId:D}.json",
                envelope.ToJson(),
                ct);
        }

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
            BundlePayloadBlobUri = bundlePayloadBlobUri
        };

        await connection.ExecuteAsync(new CommandDefinition(sql, args, transaction, cancellationToken: ct));

        ArtifactBundlePersistContext persistContext = new(blobStore, payloadOpts);

        await InsertArtifactBundleArtifactsRelationalAsync(bundle, connection, transaction, ct, persistContext);
        await InsertArtifactBundleTraceRelationalAsync(bundle, connection, transaction, ct);
    }

    private static async Task InsertArtifactBundleArtifactsRelationalAsync(
        ArtifactBundle bundle,
        IDbConnection connection,
        IDbTransaction? transaction,
        CancellationToken ct,
        ArtifactBundlePersistContext? persistContext = null)
    {
        Guid bundleId = bundle.BundleId;

        const string insertArtifactSql = """
                                         INSERT INTO dbo.ArtifactBundleArtifacts
                                         (
                                             BundleId, SortOrder, ArtifactId, RunId, ManifestId, CreatedUtc,
                                             ArtifactType, Name, Format, Content, ContentHash, ContentBlobUri
                                         )
                                         VALUES
                                         (
                                             @BundleId, @SortOrder, @ArtifactId, @RunId, @ManifestId, @CreatedUtc,
                                             @ArtifactType, @Name, @Format, @Content, @ContentHash, @ContentBlobUri
                                         );
                                         """;

        for (int i = 0; i < bundle.Artifacts.Count; i++)
        {
            SynthesizedArtifact a = bundle.Artifacts[i];

            string content = a.Content;
            string? contentBlobUri = null;

            if (persistContext is { } ctx
                && LargePayloadOffloadEvaluator.ShouldOffloadArtifactContent(ctx.Options, content.Length))
            {
                contentBlobUri = await ctx.BlobStore.WriteAsync(
                    "artifact-contents",
                    $"{bundleId:D}/{a.ArtifactId:D}.txt",
                    content,
                    ct);
                content = string.Empty;
            }

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
                        Content = content,
                        a.ContentHash,
                        ContentBlobUri = contentBlobUri
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
                            MetaValue = meta.Value
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
                            DecisionId = a.ContributingDecisionIds[d]
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
                    new { BundleId = bundleId, SortOrder = g, GeneratorName = trace.GeneratorsUsed[g] },
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
                    new { BundleId = bundleId, SortOrder = s, DecisionId = trace.SourceDecisionIds[s] },
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
                    new { BundleId = bundleId, SortOrder = n, NoteText = trace.Notes[n] },
                    transaction,
                    cancellationToken: ct));
        }
    }

    /// <summary>
    ///     Loads a bundle by primary key (admin/backfill scenarios).
    /// </summary>
    public async Task<ArtifactBundle?> GetByBundleIdAsync(Guid bundleId, CancellationToken ct)
    {
        await using SqlConnection connection = await connectionFactory.CreateOpenConnectionAsync(ct);
        return await GetByBundleIdAsync(bundleId, connection, null, ct);
    }

    /// <inheritdoc cref="GetByBundleIdAsync(System.Guid,System.Threading.CancellationToken)" />
    public async Task<ArtifactBundle?> GetByBundleIdAsync(
        Guid bundleId,
        IDbConnection connection,
        IDbTransaction? transaction,
        CancellationToken ct)
    {
        const string sql = """
                           SELECT
                               TenantId, WorkspaceId, ProjectId,
                               BundleId, RunId, ManifestId, CreatedUtc, ArtifactsJson, TraceJson, BundlePayloadBlobUri
                           FROM dbo.ArtifactBundles
                           WHERE BundleId = @BundleId;
                           """;

        ArtifactBundleStorageRow? row = await connection.QuerySingleOrDefaultAsync<ArtifactBundleStorageRow>(
            new CommandDefinition(
                sql,
                new { BundleId = bundleId },
                transaction,
                cancellationToken: ct));

        if (row is null)
            return null;

        SqlConnection sqlConnection = connection as SqlConnection
                                      ?? throw new InvalidOperationException(
                                          "SQL Server backfill requires SqlConnection.");

        row = await ApplyBundleBlobOverlayIfPresentAsync(row, ct);

        return await ArtifactBundleRelationalRead.HydrateBundleAsync(sqlConnection, row, blobStore, ct);
    }

    /// <summary>
    ///     Inserts relational artifact/trace slices that are still empty while JSON columns contain data (idempotent per
    ///     slice).
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

        int artifactRowCount = await SqlRelationalScalarCount.ExecuteAsync(
            connection,
            transaction,
            "SELECT COUNT(1) FROM dbo.ArtifactBundleArtifacts WHERE BundleId = @BundleId",
            new { BundleId = bundleId },
            ct);

        int genCount = await SqlRelationalScalarCount.ExecuteAsync(
            connection,
            transaction,
            "SELECT COUNT(1) FROM dbo.ArtifactBundleTraceGenerators WHERE BundleId = @BundleId",
            new { BundleId = bundleId },
            ct);

        int traceDecCount = await SqlRelationalScalarCount.ExecuteAsync(
            connection,
            transaction,
            "SELECT COUNT(1) FROM dbo.ArtifactBundleTraceDecisionLinks WHERE BundleId = @BundleId",
            new { BundleId = bundleId },
            ct);

        int notesCount = await SqlRelationalScalarCount.ExecuteAsync(
            connection,
            transaction,
            "SELECT COUNT(1) FROM dbo.ArtifactBundleTraceNotes WHERE BundleId = @BundleId",
            new { BundleId = bundleId },
            ct);

        if (artifactRowCount == 0 && bundle.Artifacts.Count > 0)
        {
            await InsertArtifactBundleArtifactsRelationalAsync(bundle, connection, transaction, ct, null);
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

    private async Task<ArtifactBundleStorageRow> ApplyBundleBlobOverlayIfPresentAsync(
        ArtifactBundleStorageRow row,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(row.BundlePayloadBlobUri))
            return row;

        string? json = await blobStore.ReadAsync(row.BundlePayloadBlobUri!, ct);

        if (string.IsNullOrEmpty(json))
            return row;

        ArtifactBundlePayloadBlobEnvelope? envelope = ArtifactBundlePayloadBlobEnvelope.TryDeserialize(json);

        if (envelope is null || envelope.SchemaVersion != ArtifactBundlePayloadBlobEnvelope.CurrentSchemaVersion)
            return row;

        return ArtifactBundlePayloadBlobEnvelope.MergeIntoRow(row, envelope);
    }
}
