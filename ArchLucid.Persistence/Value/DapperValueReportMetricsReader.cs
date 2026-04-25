using ArchLucid.Persistence.Connections;

using Dapper;

using Microsoft.Data.SqlClient;

namespace ArchLucid.Persistence.Value;

public sealed class DapperValueReportMetricsReader(ISqlConnectionFactory connectionFactory) : IValueReportMetricsReader
{
    private readonly ISqlConnectionFactory _connectionFactory =
        connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));

    public async Task<ValueReportRawMetrics> ReadAsync(
        Guid tenantId,
        Guid workspaceId,
        Guid projectId,
        DateTimeOffset fromUtcInclusive,
        DateTimeOffset toUtcExclusive,
        CancellationToken cancellationToken)
    {
        await using SqlConnection connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        const string runsByStatusSql = """
                                       SELECT COALESCE(LegacyRunStatus, '(unknown)') AS LegacyRunStatusLabel, COUNT_BIG(*) AS Cnt
                                       FROM dbo.Runs
                                       WHERE TenantId = @TenantId
                                         AND WorkspaceId = @WorkspaceId
                                         AND ScopeProjectId = @ProjectId
                                         AND CreatedUtc >= @FromUtc
                                         AND CreatedUtc < @ToUtc
                                         AND ArchivedUtc IS NULL
                                       GROUP BY LegacyRunStatus
                                       """;

        const string runsCompletedSql = """
                                        SELECT COUNT_BIG(*)
                                        FROM dbo.Runs
                                        WHERE TenantId = @TenantId
                                          AND WorkspaceId = @WorkspaceId
                                          AND ScopeProjectId = @ProjectId
                                          AND CreatedUtc >= @FromUtc
                                          AND CreatedUtc < @ToUtc
                                          AND ArchivedUtc IS NULL
                                          AND CompletedUtc IS NOT NULL
                                        """;

        const string manifestsSql = """
                                    SELECT COUNT_BIG(*)
                                    FROM dbo.GoldenManifests
                                    WHERE TenantId = @TenantId
                                      AND WorkspaceId = @WorkspaceId
                                      AND ProjectId = @ProjectId
                                      AND CreatedUtc >= @FromUtc
                                      AND CreatedUtc < @ToUtc
                                      AND (ArchivedUtc IS NULL)
                                    """;

        const string governanceSql = """
                                     SELECT COUNT_BIG(*)
                                     FROM dbo.AuditEvents
                                     WHERE TenantId = @TenantId
                                       AND WorkspaceId = @WorkspaceId
                                       AND ProjectId = @ProjectId
                                       AND OccurredUtc >= @FromUtc
                                       AND OccurredUtc < @ToUtc
                                       AND EventType IN @GovTypes
                                     """;

        const string driftSql = """
                                SELECT COUNT_BIG(*)
                                FROM dbo.AuditEvents
                                WHERE TenantId = @TenantId
                                  AND WorkspaceId = @WorkspaceId
                                  AND ProjectId = @ProjectId
                                  AND OccurredUtc >= @FromUtc
                                  AND OccurredUtc < @ToUtc
                                  AND EventType IN @DriftTypes
                                """;

        object parameters = new
        {
            TenantId = tenantId,
            WorkspaceId = workspaceId,
            ProjectId = projectId,
            FromUtc = fromUtcInclusive.UtcDateTime,
            ToUtc = toUtcExclusive.UtcDateTime,
            GovTypes = ValueReportMetricEventTypes.GovernanceEventTypes,
            DriftTypes = ValueReportMetricEventTypes.DriftAlertEventTypes
        };

        IEnumerable<RunStatusSqlRow> rows = await connection.QueryAsync<RunStatusSqlRow>(
            new CommandDefinition(runsByStatusSql, parameters, cancellationToken: cancellationToken));

        List<ValueReportRunStatusCount> statusCounts = rows
            .Select(static r =>
                new ValueReportRunStatusCount(r.LegacyRunStatusLabel, (int)Math.Min(int.MaxValue, r.Cnt)))
            .ToList();

        long runsCompleted = await connection.QuerySingleAsync<long>(
            new CommandDefinition(runsCompletedSql, parameters, cancellationToken: cancellationToken));

        long manifests = await connection.QuerySingleAsync<long>(
            new CommandDefinition(manifestsSql, parameters, cancellationToken: cancellationToken));

        long governance = await connection.QuerySingleAsync<long>(
            new CommandDefinition(governanceSql, parameters, cancellationToken: cancellationToken));

        long drift = await connection.QuerySingleAsync<long>(
            new CommandDefinition(driftSql, parameters, cancellationToken: cancellationToken));

        const string findingFeedbackSql = """
                                          SELECT COALESCE(SUM(CAST(Score AS BIGINT)), 0) AS NetScore, COUNT_BIG(*) AS VoteCount
                                          FROM dbo.FindingFeedback
                                          WHERE TenantId = @TenantId
                                            AND WorkspaceId = @WorkspaceId
                                            AND ProjectId = @ProjectId
                                            AND CreatedUtc >= @FromUtc
                                            AND CreatedUtc < @ToUtc;
                                          """;

        FindingFeedbackAggRow feedbackAgg = await connection.QuerySingleAsync<FindingFeedbackAggRow>(
            new CommandDefinition(findingFeedbackSql, parameters, cancellationToken: cancellationToken));

        const string tenantBaselineSql = """
                                         SELECT BaselineReviewCycleHours,
                                                BaselineReviewCycleSource,
                                                BaselineReviewCycleCapturedUtc,
                                                BaselineManualPrepHoursPerReview,
                                                BaselinePeoplePerReview,
                                                ArchitectureTeamSize
                                         FROM dbo.Tenants
                                         WHERE Id = @TenantId;
                                         """;

        TenantBaselineRow? tenantBaseline = await connection.QuerySingleOrDefaultAsync<TenantBaselineRow>(
            new CommandDefinition(
                tenantBaselineSql,
                new { TenantId = tenantId },
                cancellationToken: cancellationToken));

        const string reviewCycleSql = """
                                      SELECT
                                          AVG(CAST(DATEDIFF(SECOND, r.CreatedUtc, m.CreatedUtc) AS DECIMAL(18, 6))) / 3600.0 AS AvgHours,
                                          COUNT_BIG(*) AS Cnt
                                      FROM dbo.GoldenManifests m
                                      INNER JOIN dbo.Runs r ON m.RunId = r.RunId
                                      WHERE m.TenantId = @TenantId
                                        AND m.WorkspaceId = @WorkspaceId
                                        AND m.ProjectId = @ProjectId
                                        AND m.CreatedUtc >= @FromUtc
                                        AND m.CreatedUtc < @ToUtc
                                        AND (m.ArchivedUtc IS NULL)
                                        AND (r.ArchivedUtc IS NULL);
                                      """;

        ReviewCycleMeasureRow measure = await connection.QuerySingleAsync<ReviewCycleMeasureRow>(
            new CommandDefinition(reviewCycleSql, parameters, cancellationToken: cancellationToken));

        decimal? measuredAvg = measure.Cnt == 0 ? null : measure.AvgHours;
        int sampleSize = measure.Cnt > int.MaxValue ? int.MaxValue : (int)measure.Cnt;

        return new ValueReportRawMetrics(
            statusCounts,
            (int)Math.Min(int.MaxValue, runsCompleted),
            (int)Math.Min(int.MaxValue, manifests),
            (int)Math.Min(int.MaxValue, governance),
            (int)Math.Min(int.MaxValue, drift),
            (int)Math.Clamp(feedbackAgg.NetScore, int.MinValue, int.MaxValue),
            (int)Math.Min(int.MaxValue, feedbackAgg.VoteCount),
            tenantBaseline?.BaselineReviewCycleHours,
            tenantBaseline?.BaselineReviewCycleSource,
            tenantBaseline?.BaselineReviewCycleCapturedUtc,
            measuredAvg,
            sampleSize,
            tenantBaseline?.BaselineManualPrepHoursPerReview,
            tenantBaseline?.BaselinePeoplePerReview,
            tenantBaseline?.ArchitectureTeamSize);
    }

    private sealed class FindingFeedbackAggRow
    {
        public long NetScore
        {
            get;
            init;
        }

        public long VoteCount
        {
            get;
            init;
        }
    }

    private sealed class TenantBaselineRow
    {
        public decimal? BaselineReviewCycleHours
        {
            get;
            init;
        }

        public string? BaselineReviewCycleSource
        {
            get;
            init;
        }

        public DateTimeOffset? BaselineReviewCycleCapturedUtc
        {
            get;
            init;
        }

        public decimal? BaselineManualPrepHoursPerReview
        {
            get;
            init;
        }

        public int? BaselinePeoplePerReview
        {
            get;
            init;
        }

        public int? ArchitectureTeamSize
        {
            get;
            init;
        }
    }

    private sealed class ReviewCycleMeasureRow
    {
        public decimal? AvgHours
        {
            get;
            init;
        }

        public long Cnt
        {
            get;
            init;
        }
    }

    private sealed class RunStatusSqlRow
    {
        public string LegacyRunStatusLabel
        {
            get;
            init;
        } = "";

        public long Cnt
        {
            get;
            init;
        }
    }
}
