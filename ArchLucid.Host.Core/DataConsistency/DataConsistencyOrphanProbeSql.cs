namespace ArchLucid.Host.Core.DataConsistency;

/// <summary>
/// SQL fragments shared by <see cref="Hosted.DataConsistencyOrphanProbeHostedService"/> and admin diagnostics (detection-only counts).
/// </summary>
public static class DataConsistencyOrphanProbeSql
{
    /// <summary>Orphan <c>ComparisonRecords</c> rows: parsable <c>LeftRunId</c> with no <c>dbo.Runs</c> match.</summary>
    public const string ComparisonRecordsLeftRunId = """
        SELECT COUNT_BIG(1)
        FROM dbo.ComparisonRecords c
        WHERE c.LeftRunId IS NOT NULL
          AND TRY_CONVERT(UNIQUEIDENTIFIER, c.LeftRunId) IS NOT NULL
          AND NOT EXISTS (
              SELECT 1
              FROM dbo.Runs r
              WHERE r.RunId = TRY_CONVERT(UNIQUEIDENTIFIER, c.LeftRunId));
        """;

    /// <summary>Orphan <c>ComparisonRecords</c> rows: parsable <c>RightRunId</c> with no <c>dbo.Runs</c> match.</summary>
    public const string ComparisonRecordsRightRunId = """
        SELECT COUNT_BIG(1)
        FROM dbo.ComparisonRecords c
        WHERE c.RightRunId IS NOT NULL
          AND TRY_CONVERT(UNIQUEIDENTIFIER, c.RightRunId) IS NOT NULL
          AND NOT EXISTS (
              SELECT 1
              FROM dbo.Runs r
              WHERE r.RunId = TRY_CONVERT(UNIQUEIDENTIFIER, c.RightRunId));
        """;

    /// <summary>Orphan <c>GoldenManifests</c> rows whose <c>RunId</c> is missing from <c>dbo.Runs</c>.</summary>
    public const string GoldenManifestsRunId = """
        SELECT COUNT_BIG(1)
        FROM dbo.GoldenManifests g
        WHERE NOT EXISTS (
            SELECT 1
            FROM dbo.Runs r
            WHERE r.RunId = g.RunId);
        """;

    /// <summary>Orphan <c>FindingsSnapshots</c> rows whose <c>RunId</c> is missing from <c>dbo.Runs</c>.</summary>
    public const string FindingsSnapshotsRunId = """
        SELECT COUNT_BIG(1)
        FROM dbo.FindingsSnapshots f
        WHERE NOT EXISTS (
            SELECT 1
            FROM dbo.Runs r
            WHERE r.RunId = f.RunId);
        """;
}
