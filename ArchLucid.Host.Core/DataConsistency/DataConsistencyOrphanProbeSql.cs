namespace ArchLucid.Host.Core.DataConsistency;

/// <summary>
/// SQL fragments shared by <see cref="Hosted.DataConsistencyOrphanProbeHostedService"/> and admin diagnostics (detection-only counts).
/// </summary>
/// <remarks>
/// <c>dbo.ComparisonRecords</c> LeftRunId/RightRunId are <c>UNIQUEIDENTIFIER</c> with FK to <c>dbo.Runs</c> (DbUp 137);
/// orphan probe no longer applies.
/// </remarks>
public static class DataConsistencyOrphanProbeSql
{
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

    /// <summary>Orphan <c>ContextSnapshots</c> rows whose <c>RunId</c> is missing from <c>dbo.Runs</c>.</summary>
    public const string ContextSnapshotsRunId = """
                                                SELECT COUNT_BIG(1)
                                                FROM dbo.ContextSnapshots c
                                                WHERE NOT EXISTS (
                                                    SELECT 1
                                                    FROM dbo.Runs r
                                                    WHERE r.RunId = c.RunId);
                                                """;

    /// <summary>Orphan <c>GraphSnapshots</c> rows whose <c>RunId</c> is missing from <c>dbo.Runs</c>.</summary>
    public const string GraphSnapshotsRunId = """
                                              SELECT COUNT_BIG(1)
                                              FROM dbo.GraphSnapshots g
                                              WHERE NOT EXISTS (
                                                  SELECT 1
                                                  FROM dbo.Runs r
                                                  WHERE r.RunId = g.RunId);
                                              """;
}
