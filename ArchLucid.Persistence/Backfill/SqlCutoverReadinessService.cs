using System.Diagnostics.CodeAnalysis;

using ArchiForge.Persistence.Connections;

using Dapper;

using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace ArchiForge.Persistence.Backfill;

/// <summary>
/// Runs aggregate read-only SQL queries to determine per-slice relational coverage.
/// Uses set-based <c>COUNT / WHERE EXISTS</c> correlated subqueries — no row-by-row iteration.
/// </summary>
[ExcludeFromCodeCoverage(Justification = "Entirely SQL-dependent; every method runs Dapper queries against live SQL Server.")]
public sealed class SqlCutoverReadinessService(
    ISqlConnectionFactory connectionFactory,
    ILogger<SqlCutoverReadinessService> logger) : ICutoverReadinessService
{
    public async Task<CutoverReadinessReport> AssessAsync(CancellationToken ct)
    {
        logger.LogInformation("Cutover readiness assessment starting.");

        await using SqlConnection connection = await connectionFactory.CreateOpenConnectionAsync(ct);

        List<CutoverSliceReadiness> slices =
        [
            .. await AssessContextSnapshotsAsync(connection, ct),
            .. await AssessGraphSnapshotsAsync(connection, ct),
            .. await AssessFindingsSnapshotsAsync(connection, ct),
            .. await AssessGoldenManifestsAsync(connection, ct),
            .. await AssessArtifactBundlesAsync(connection, ct),
        ];

        CutoverReadinessReport report = new() { Slices = slices };

        logger.LogInformation(
            "Cutover readiness assessment complete. TotalSlices={SliceCount}, Ready={ReadyCount}, NotReady={NotReadyCount}",
            slices.Count,
            slices.Count(static s => s.IsReady),
            report.SlicesNotReady.Count);

        return report;
    }

    private static async Task<List<CutoverSliceReadiness>> AssessContextSnapshotsAsync(
        SqlConnection connection, CancellationToken ct)
    {
        int totalHeaders = await CountAsync(connection,
            "SELECT COUNT(1) FROM dbo.ContextSnapshots;", ct);

        return
        [
            await AssessSliceAsync(connection, "ContextSnapshot.CanonicalObjects", totalHeaders,
                CountHeadersWithChildrenSql("ContextSnapshots", "SnapshotId", "ContextSnapshotCanonicalObjects", "SnapshotId"), ct),

            await AssessSliceAsync(connection, "ContextSnapshot.Warnings", totalHeaders,
                CountHeadersWithChildrenSql("ContextSnapshots", "SnapshotId", "ContextSnapshotWarnings", "SnapshotId"), ct),

            await AssessSliceAsync(connection, "ContextSnapshot.Errors", totalHeaders,
                CountHeadersWithChildrenSql("ContextSnapshots", "SnapshotId", "ContextSnapshotErrors", "SnapshotId"), ct),

            await AssessSliceAsync(connection, "ContextSnapshot.SourceHashes", totalHeaders,
                CountHeadersWithChildrenSql("ContextSnapshots", "SnapshotId", "ContextSnapshotSourceHashes", "SnapshotId"), ct),
        ];
    }

    private static async Task<List<CutoverSliceReadiness>> AssessGraphSnapshotsAsync(
        SqlConnection connection, CancellationToken ct)
    {
        int totalHeaders = await CountAsync(connection,
            "SELECT COUNT(1) FROM dbo.GraphSnapshots;", ct);

        return
        [
            await AssessSliceAsync(connection, "GraphSnapshot.Nodes", totalHeaders,
                CountHeadersWithChildrenSql("GraphSnapshots", "GraphSnapshotId", "GraphSnapshotNodes", "GraphSnapshotId"), ct),

            await AssessSliceAsync(connection, "GraphSnapshot.Edges", totalHeaders,
                CountHeadersWithChildrenSql("GraphSnapshots", "GraphSnapshotId", "GraphSnapshotEdges", "GraphSnapshotId"), ct),

            await AssessSliceAsync(connection, "GraphSnapshot.Warnings", totalHeaders,
                CountHeadersWithChildrenSql("GraphSnapshots", "GraphSnapshotId", "GraphSnapshotWarnings", "GraphSnapshotId"), ct),

            await AssessSliceAsync(connection, "GraphSnapshot.EdgeProperties", totalHeaders,
                CountHeadersWithChildrenSql("GraphSnapshots", "GraphSnapshotId", "GraphSnapshotEdgeProperties", "GraphSnapshotId"), ct),
        ];
    }

    private static async Task<List<CutoverSliceReadiness>> AssessFindingsSnapshotsAsync(
        SqlConnection connection, CancellationToken ct)
    {
        int totalHeaders = await CountAsync(connection,
            "SELECT COUNT(1) FROM dbo.FindingsSnapshots;", ct);

        return
        [
            await AssessSliceAsync(connection, "FindingsSnapshot.Findings", totalHeaders,
                CountHeadersWithChildrenSql("FindingsSnapshots", "FindingsSnapshotId", "FindingRecords", "FindingsSnapshotId"), ct),
        ];
    }

    private static async Task<List<CutoverSliceReadiness>> AssessGoldenManifestsAsync(
        SqlConnection connection, CancellationToken ct)
    {
        int totalHeaders = await CountAsync(connection,
            "SELECT COUNT(1) FROM dbo.GoldenManifests;", ct);

        return
        [
            await AssessSliceAsync(connection, "GoldenManifest.Assumptions", totalHeaders,
                CountHeadersWithChildrenSql("GoldenManifests", "ManifestId", "GoldenManifestAssumptions", "ManifestId"), ct),

            await AssessSliceAsync(connection, "GoldenManifest.Warnings", totalHeaders,
                CountHeadersWithChildrenSql("GoldenManifests", "ManifestId", "GoldenManifestWarnings", "ManifestId"), ct),

            await AssessSliceAsync(connection, "GoldenManifest.Decisions", totalHeaders,
                CountHeadersWithChildrenSql("GoldenManifests", "ManifestId", "GoldenManifestDecisions", "ManifestId"), ct),

            await AssessSliceAsync(connection, "GoldenManifest.Provenance", totalHeaders,
                CountHeadersWithAnyProvenanceChildSql(), ct),
        ];
    }

    private static async Task<List<CutoverSliceReadiness>> AssessArtifactBundlesAsync(
        SqlConnection connection, CancellationToken ct)
    {
        int totalHeaders = await CountAsync(connection,
            "SELECT COUNT(1) FROM dbo.ArtifactBundles;", ct);

        return
        [
            await AssessSliceAsync(connection, "ArtifactBundle.Artifacts", totalHeaders,
                CountHeadersWithChildrenSql("ArtifactBundles", "BundleId", "ArtifactBundleArtifacts", "BundleId"), ct),
        ];
    }

    /// <summary>
    /// Builds a SQL statement that counts how many header rows have at least one child row
    /// in the specified child table, using a <c>WHERE EXISTS</c> correlated subquery.
    /// </summary>
    private static string CountHeadersWithChildrenSql(
        string headerTable, string headerKey, string childTable, string childKey)
    {
        return $"""
            SELECT COUNT(1)
            FROM dbo.{headerTable} h
            WHERE EXISTS (
                SELECT 1 FROM dbo.{childTable} c WHERE c.{childKey} = h.{headerKey}
            );
            """;
    }

    /// <summary>
    /// Provenance is spread across three child tables; a header is "covered" when it has
    /// at least one row in <em>any</em> of the three.
    /// </summary>
    private static string CountHeadersWithAnyProvenanceChildSql()
    {
        return """
            SELECT COUNT(1)
            FROM dbo.GoldenManifests h
            WHERE EXISTS (
                SELECT 1 FROM dbo.GoldenManifestProvenanceSourceFindings c WHERE c.ManifestId = h.ManifestId
            )
            OR EXISTS (
                SELECT 1 FROM dbo.GoldenManifestProvenanceSourceGraphNodes c WHERE c.ManifestId = h.ManifestId
            )
            OR EXISTS (
                SELECT 1 FROM dbo.GoldenManifestProvenanceAppliedRules c WHERE c.ManifestId = h.ManifestId
            );
            """;
    }

    private static async Task<CutoverSliceReadiness> AssessSliceAsync(
        SqlConnection connection, string sliceName, int totalHeaders, string countSql, CancellationToken ct)
    {
        int headersWithChildren = await CountAsync(connection, countSql, ct);

        return new CutoverSliceReadiness
        {
            SliceName = sliceName,
            TotalHeaderRows = totalHeaders,
            HeadersWithRelationalRows = headersWithChildren,
        };
    }

    private static async Task<int> CountAsync(SqlConnection connection, string sql, CancellationToken ct)
    {
        return await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(sql, cancellationToken: ct));
    }
}
