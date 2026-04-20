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
            DriftTypes = ValueReportMetricEventTypes.DriftAlertEventTypes,
        };

        IEnumerable<RunStatusSqlRow> rows = await connection.QueryAsync<RunStatusSqlRow>(
            new CommandDefinition(runsByStatusSql, parameters, cancellationToken: cancellationToken));

        List<ValueReportRunStatusCount> statusCounts = rows
            .Select(static r => new ValueReportRunStatusCount(r.LegacyRunStatusLabel, (int)Math.Min(int.MaxValue, r.Cnt)))
            .ToList();

        long runsCompleted = await connection.QuerySingleAsync<long>(
            new CommandDefinition(runsCompletedSql, parameters, cancellationToken: cancellationToken));

        long manifests = await connection.QuerySingleAsync<long>(
            new CommandDefinition(manifestsSql, parameters, cancellationToken: cancellationToken));

        long governance = await connection.QuerySingleAsync<long>(
            new CommandDefinition(governanceSql, parameters, cancellationToken: cancellationToken));

        long drift = await connection.QuerySingleAsync<long>(
            new CommandDefinition(driftSql, parameters, cancellationToken: cancellationToken));

        return new ValueReportRawMetrics(
            statusCounts,
            (int)Math.Min(int.MaxValue, runsCompleted),
            (int)Math.Min(int.MaxValue, manifests),
            (int)Math.Min(int.MaxValue, governance),
            (int)Math.Min(int.MaxValue, drift));
    }

    private sealed class RunStatusSqlRow
    {
        public string LegacyRunStatusLabel { get; init; } = "";

        public long Cnt { get; init; }
    }
}
