namespace ArchLucid.Application.DataConsistency;

/// <summary>Read-only SQL probes for scheduled reconciliation (SQL Server).</summary>
/// <remarks>Orphan fragments align with <c>ArchLucid.Host.Core.DataConsistency.DataConsistencyOrphanProbeSql</c>.</remarks>
internal static class DataConsistencyReconciliationSql
{
    internal const string ComparisonRecordsLeftRunId = """
                                                       SELECT COUNT_BIG(1)
                                                       FROM dbo.ComparisonRecords c
                                                       WHERE c.LeftRunId IS NOT NULL
                                                         AND TRY_CONVERT(UNIQUEIDENTIFIER, c.LeftRunId) IS NOT NULL
                                                         AND NOT EXISTS (
                                                             SELECT 1
                                                             FROM dbo.Runs r
                                                             WHERE r.RunId = TRY_CONVERT(UNIQUEIDENTIFIER, c.LeftRunId));
                                                       """;

    internal const string ComparisonRecordsRightRunId = """
                                                        SELECT COUNT_BIG(1)
                                                        FROM dbo.ComparisonRecords c
                                                        WHERE c.RightRunId IS NOT NULL
                                                          AND TRY_CONVERT(UNIQUEIDENTIFIER, c.RightRunId) IS NOT NULL
                                                          AND NOT EXISTS (
                                                              SELECT 1
                                                              FROM dbo.Runs r
                                                              WHERE r.RunId = TRY_CONVERT(UNIQUEIDENTIFIER, c.RightRunId));
                                                        """;

    internal const string RunsMissingArchitectureRequest = """
                                                           SELECT COUNT_BIG(1)
                                                           FROM dbo.Runs r
                                                           WHERE r.ArchivedUtc IS NULL
                                                             AND r.ArchitectureRequestId IS NOT NULL
                                                             AND LEN(LTRIM(RTRIM(r.ArchitectureRequestId))) > 0
                                                             AND NOT EXISTS (
                                                                 SELECT 1
                                                                 FROM dbo.ArchitectureRequests a
                                                                 WHERE a.RequestId = r.ArchitectureRequestId);
                                                           """;

    internal const string GoldenManifestsRunId = """
                                                 SELECT COUNT_BIG(1)
                                                 FROM dbo.GoldenManifests g
                                                 WHERE NOT EXISTS (
                                                     SELECT 1
                                                     FROM dbo.Runs r
                                                     WHERE r.RunId = g.RunId);
                                                 """;

    internal const string FindingsSnapshotsRunId = """
                                                   SELECT COUNT_BIG(1)
                                                   FROM dbo.FindingsSnapshots f
                                                   WHERE NOT EXISTS (
                                                       SELECT 1
                                                       FROM dbo.Runs r
                                                       WHERE r.RunId = f.RunId);
                                                   """;

    internal const string ArtifactBundlesRunId = """
                                                 SELECT COUNT_BIG(1)
                                                 FROM dbo.ArtifactBundles ab
                                                 WHERE NOT EXISTS (
                                                     SELECT 1
                                                     FROM dbo.Runs r
                                                     WHERE r.RunId = ab.RunId);
                                                 """;

    internal const string StaleInFlightRuns = """
                                              SELECT COUNT_BIG(1)
                                              FROM dbo.Runs r
                                              WHERE r.ArchivedUtc IS NULL
                                                AND r.LegacyRunStatus IN (N'Created', N'TasksGenerated', N'WaitingForResults', N'Retrying')
                                                AND r.CreatedUtc < DATEADD(HOUR, -1, SYSUTCDATETIME());
                                              """;

    internal const string SampleStaleRunIds = """
                                              SELECT TOP (50) CAST(r.RunId AS NVARCHAR(36))
                                              FROM dbo.Runs r
                                              WHERE r.ArchivedUtc IS NULL
                                                AND r.LegacyRunStatus IN (N'Created', N'TasksGenerated', N'WaitingForResults', N'Retrying')
                                                AND r.CreatedUtc < DATEADD(HOUR, -1, SYSUTCDATETIME())
                                              ORDER BY r.CreatedUtc ASC;
                                              """;

    internal const string RecentRunsForCacheSample = """
                                                     SELECT TOP (10)
                                                         r.TenantId,
                                                         r.WorkspaceId,
                                                         r.ScopeProjectId,
                                                         r.RunId,
                                                         r.LegacyRunStatus,
                                                         r.CurrentManifestVersion,
                                                         r.CompletedUtc,
                                                         r.CreatedUtc
                                                     FROM dbo.Runs r
                                                     WHERE r.ArchivedUtc IS NULL
                                                     ORDER BY r.CreatedUtc DESC;
                                                     """;

    internal const string SampleRunsMissingArchitectureRequest = """
                                                                 SELECT TOP (50) CAST(r.RunId AS NVARCHAR(36))
                                                                 FROM dbo.Runs r
                                                                 WHERE r.ArchivedUtc IS NULL
                                                                   AND r.ArchitectureRequestId IS NOT NULL
                                                                   AND LEN(LTRIM(RTRIM(r.ArchitectureRequestId))) > 0
                                                                   AND NOT EXISTS (
                                                                       SELECT 1
                                                                       FROM dbo.ArchitectureRequests a
                                                                       WHERE a.RequestId = r.ArchitectureRequestId)
                                                                 ORDER BY r.CreatedUtc DESC;
                                                                 """;

    internal const string SampleGoldenManifestOrphans = """
                                                        SELECT TOP (50) CAST(g.ManifestId AS NVARCHAR(36))
                                                        FROM dbo.GoldenManifests g
                                                        WHERE NOT EXISTS (
                                                            SELECT 1
                                                            FROM dbo.Runs r
                                                            WHERE r.RunId = g.RunId)
                                                        ORDER BY g.CreatedUtc DESC;
                                                        """;

    internal const string SampleFindingsSnapshotOrphans = """
                                                          SELECT TOP (50) CAST(f.FindingsSnapshotId AS NVARCHAR(36))
                                                          FROM dbo.FindingsSnapshots f
                                                          WHERE NOT EXISTS (
                                                              SELECT 1
                                                              FROM dbo.Runs r
                                                              WHERE r.RunId = f.RunId)
                                                          ORDER BY f.CreatedUtc DESC;
                                                          """;

    internal const string SampleArtifactBundleOrphans = """
                                                        SELECT TOP (50) CAST(ab.BundleId AS NVARCHAR(36))
                                                        FROM dbo.ArtifactBundles ab
                                                        WHERE NOT EXISTS (
                                                            SELECT 1
                                                            FROM dbo.Runs r
                                                            WHERE r.RunId = ab.RunId)
                                                        ORDER BY ab.CreatedUtc DESC;
                                                        """;

    internal const string SampleComparisonRecordsLeftOrphans = """
                                                               SELECT TOP (50) c.ComparisonRecordId
                                                               FROM dbo.ComparisonRecords c
                                                               WHERE c.LeftRunId IS NOT NULL
                                                                 AND TRY_CONVERT(UNIQUEIDENTIFIER, c.LeftRunId) IS NOT NULL
                                                                 AND NOT EXISTS (
                                                                     SELECT 1
                                                                     FROM dbo.Runs r
                                                                     WHERE r.RunId = TRY_CONVERT(UNIQUEIDENTIFIER, c.LeftRunId))
                                                               ORDER BY c.CreatedUtc ASC;
                                                               """;

    internal const string SampleComparisonRecordsRightOrphans = """
                                                                SELECT TOP (50) c.ComparisonRecordId
                                                                FROM dbo.ComparisonRecords c
                                                                WHERE c.RightRunId IS NOT NULL
                                                                  AND TRY_CONVERT(UNIQUEIDENTIFIER, c.RightRunId) IS NOT NULL
                                                                  AND NOT EXISTS (
                                                                      SELECT 1
                                                                      FROM dbo.Runs r
                                                                      WHERE r.RunId = TRY_CONVERT(UNIQUEIDENTIFIER, c.RightRunId))
                                                                ORDER BY c.CreatedUtc ASC;
                                                                """;
}
