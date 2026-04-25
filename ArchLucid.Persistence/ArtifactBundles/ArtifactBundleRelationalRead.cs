using ArchLucid.ArtifactSynthesis.Models;
using ArchLucid.Persistence.BlobStore;
using ArchLucid.Persistence.RelationalRead;

using Dapper;

using Microsoft.Data.SqlClient;

namespace ArchLucid.Persistence.ArtifactBundles;

/// <summary>
///     Relational hydration for artifact list slices when rows exist; otherwise <c>ArtifactsJson</c>. Trace base
///     remains JSON with relational list overlays.
/// </summary>
internal static class ArtifactBundleRelationalRead
{
    internal static async Task<ArtifactBundle> HydrateBundleAsync(
        SqlConnection connection,
        ArtifactBundleStorageRow row,
        IArtifactBlobStore blobStore,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(blobStore);

        Guid bundleId = row.BundleId;

        int artifactCount = await SqlRelationalScalarCount.ExecuteAsync(
            connection,
            null,
            "SELECT COUNT(1) FROM dbo.ArtifactBundleArtifacts WHERE BundleId = @BundleId",
            new
            {
                BundleId = bundleId
            },
            ct);

        int genCount = await SqlRelationalScalarCount.ExecuteAsync(
            connection,
            null,
            "SELECT COUNT(1) FROM dbo.ArtifactBundleTraceGenerators WHERE BundleId = @BundleId",
            new
            {
                BundleId = bundleId
            },
            ct);

        int traceDecCount = await SqlRelationalScalarCount.ExecuteAsync(
            connection,
            null,
            "SELECT COUNT(1) FROM dbo.ArtifactBundleTraceDecisionLinks WHERE BundleId = @BundleId",
            new
            {
                BundleId = bundleId
            },
            ct);

        int notesCount = await SqlRelationalScalarCount.ExecuteAsync(
            connection,
            null,
            "SELECT COUNT(1) FROM dbo.ArtifactBundleTraceNotes WHERE BundleId = @BundleId",
            new
            {
                BundleId = bundleId
            },
            ct);

        List<SynthesizedArtifact> artifacts = artifactCount > 0
            ? await LoadArtifactsRelationalAsync(connection, bundleId, ct)
            : ArtifactBundleArtifactsJsonReader.DeserializeArtifacts(row.ArtifactsJson);

        SynthesisTrace trace = ArtifactBundleTraceJsonReader.DeserializeTraceBase(row.TraceJson);

        if (genCount > 0)

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


        if (traceDecCount > 0)

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


        if (notesCount > 0)

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
            Trace = trace
        };
    }

    private static async Task<List<SynthesizedArtifact>> LoadArtifactsRelationalAsync(
        SqlConnection connection,
        Guid bundleId,
        CancellationToken ct)
    {
        const string artifactsSql = """
                                    SELECT SortOrder, ArtifactId, RunId, ManifestId, CreatedUtc,
                                           ArtifactType, Name, Format, Content, ContentHash, ContentBlobUri
                                    FROM dbo.ArtifactBundleArtifacts
                                    WHERE BundleId = @BundleId
                                    ORDER BY SortOrder;
                                    """;

        List<ArtifactSliceRow> artifactRows = (await connection.QueryAsync<ArtifactSliceRow>(
            new CommandDefinition(
                artifactsSql,
                new
                {
                    BundleId = bundleId
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
                    BundleId = bundleId
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
                    BundleId = bundleId
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
                    ContributingDecisionIds = decIds
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
                    BundleId = bundleId
                },
                cancellationToken: ct));

        return rows.ToList();
    }

    private sealed class ArtifactSliceRow
    {
        public int SortOrder
        {
            get;
            init;
        }

        public Guid ArtifactId
        {
            get;
            init;
        }

        public Guid RunId
        {
            get;
            init;
        }

        public Guid ManifestId
        {
            get;
            init;
        }

        public DateTime CreatedUtc
        {
            get;
            init;
        }

        public string ArtifactType
        {
            get;
            init;
        } = null!;

        public string Name
        {
            get;
            init;
        } = null!;

        public string Format
        {
            get;
            init;
        } = null!;

        public string? Content
        {
            get;
            init;
        }

        public string ContentHash
        {
            get;
            init;
        } = null!;

        public string? ContentBlobUri
        {
            get;
            init;
        }
    }

    private sealed class MetadataSliceRow
    {
        public int ArtifactSortOrder
        {
            get;
            init;
        }

        public int MetaSortOrder
        {
            get;
            init;
        }

        public string MetaKey
        {
            get;
            init;
        } = null!;

        public string MetaValue
        {
            get;
            init;
        } = null!;
    }

    private sealed class ArtifactDecisionSliceRow
    {
        public int ArtifactSortOrder
        {
            get;
            init;
        }

        public int LinkSortOrder
        {
            get;
            init;
        }

        public string DecisionId
        {
            get;
            init;
        } = null!;
    }
}
