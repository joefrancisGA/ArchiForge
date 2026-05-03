namespace ArchLucid.Host.Core.DataConsistency;

/// <summary>
/// SQL for operator-initiated remediation of orphan rows detected by legacy probes (see <see cref="DataConsistencyOrphanProbeSql"/>).
/// </summary>
/// <remarks>
/// <c>dbo.ComparisonRecords</c> run ids are FK-backed (DbUp 137); the historical comparison-record orphan SQL is retained as a no-op
/// so admin callers keep stable command shapes.
/// </remarks>
public static class DataConsistencyOrphanRemediationSql
{
    /// <summary>Always empty: referential integrity is enforced by FK on <c>LeftRunId</c>/<c>RightRunId</c>.</summary>
    public const string SelectOrphanComparisonRecordIds = """
                                                          SELECT TOP (@MaxRows) c.ComparisonRecordId
                                                          FROM dbo.ComparisonRecords c
                                                          WHERE 1 = 0
                                                          ORDER BY c.CreatedUtc ASC;
                                                          """;

    /// <summary>No-op delete (matches the empty select predicate).</summary>
    public const string DeleteOrphanComparisonRecordsWithOutput = """
                                                                  WITH cte AS (
                                                                      SELECT TOP (@MaxRows) c.ComparisonRecordId
                                                                      FROM dbo.ComparisonRecords c
                                                                      WHERE 1 = 0
                                                                      ORDER BY c.CreatedUtc ASC
                                                                  )
                                                                  DELETE c
                                                                  OUTPUT deleted.ComparisonRecordId
                                                                  FROM dbo.ComparisonRecords AS c
                                                                  INNER JOIN cte ON c.ComparisonRecordId = cte.ComparisonRecordId;
                                                                  """;

    /// <summary>
    /// Lists up to <c>@MaxRows</c> orphan <c>dbo.GoldenManifests.ManifestId</c> values (no <c>dbo.Runs</c> for <c>RunId</c>), oldest first.
    /// </summary>
    public const string SelectOrphanGoldenManifestIds = """
                                                        SELECT TOP (@MaxRows) g.ManifestId
                                                        FROM dbo.GoldenManifests g
                                                        WHERE NOT EXISTS (
                                                            SELECT 1
                                                            FROM dbo.Runs r
                                                            WHERE r.RunId = g.RunId)
                                                        ORDER BY g.CreatedUtc ASC;
                                                        """;

    /// <summary>
    /// Lists orphan <c>dbo.FindingsSnapshots</c> (missing <c>Runs</c>, not referenced by any <c>GoldenManifests</c>), oldest first.
    /// </summary>
    public const string SelectOrphanFindingsSnapshotIds = """
                                                          SELECT TOP (@MaxRows) f.FindingsSnapshotId
                                                          FROM dbo.FindingsSnapshots f
                                                          WHERE NOT EXISTS (
                                                              SELECT 1
                                                              FROM dbo.Runs r
                                                              WHERE r.RunId = f.RunId)
                                                            AND NOT EXISTS (
                                                              SELECT 1
                                                              FROM dbo.GoldenManifests g
                                                              WHERE g.FindingsSnapshotId = f.FindingsSnapshotId)
                                                          ORDER BY f.CreatedUtc ASC;
                                                          """;

    /// <summary>
    /// Soft deletes orphan <c>dbo.GraphSnapshots</c> and <c>dbo.GraphSnapshotEdges</c>.
    /// </summary>
    public const string SoftDeleteOrphanGraphSnapshotsWithOutput = """
                                                                   WITH cte AS (
                                                                       SELECT TOP (@MaxRows) g.GraphSnapshotId
                                                                       FROM dbo.GraphSnapshots g
                                                                       WHERE g.IsDeleted = 0
                                                                         AND NOT EXISTS (
                                                                           SELECT 1
                                                                           FROM dbo.Runs r
                                                                           WHERE r.RunId = g.RunId)
                                                                       ORDER BY g.CreatedUtc ASC
                                                                   )
                                                                   UPDATE g
                                                                   SET g.IsDeleted = 1
                                                                   OUTPUT inserted.GraphSnapshotId
                                                                   FROM dbo.GraphSnapshots AS g
                                                                   INNER JOIN cte ON g.GraphSnapshotId = cte.GraphSnapshotId;
                                                                   """;
}
