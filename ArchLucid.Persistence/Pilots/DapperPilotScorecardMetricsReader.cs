using ArchLucid.Persistence.Connections;

using Dapper;

using Microsoft.Data.SqlClient;

namespace ArchLucid.Persistence.Pilots;

public sealed class DapperPilotScorecardMetricsReader(ISqlConnectionFactory connectionFactory) : IPilotScorecardMetricsReader
{
    private readonly ISqlConnectionFactory _connectionFactory =
        connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));

    public async Task<PilotScorecardTenantMetrics> GetAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        const string sql = """
                             SELECT
                                 (SELECT COUNT(*)
                                  FROM dbo.Runs r
                                  WHERE r.TenantId = @TenantId
                                    AND r.ArchivedUtc IS NULL
                                    AND (
                                         (NULLIF(LTRIM(RTRIM(r.CurrentManifestVersion)), N'') IS NOT NULL)
                                         OR (r.GoldenManifestId IS NOT NULL)
                                     )) AS TotalRunsCommitted,
                                 (SELECT COUNT(*)
                                  FROM dbo.GoldenManifests gm
                                  INNER JOIN dbo.Runs r ON r.RunId = gm.RunId
                                  WHERE r.TenantId = @TenantId
                                    AND r.ArchivedUtc IS NULL) AS TotalManifestsCreated,
                                 (SELECT COUNT(*)
                                  FROM dbo.FindingFeedback ff
                                  WHERE ff.TenantId = @TenantId
                                    AND ff.Score = 1) AS TotalFindingsResolved,
                                 (SELECT AVG(CAST(DATEDIFF_BIG(MINUTE, r.CreatedUtc, gm.CreatedUtc) AS FLOAT))
                                  FROM dbo.Runs r
                                  INNER JOIN dbo.GoldenManifests gm ON gm.RunId = r.RunId
                                  WHERE r.TenantId = @TenantId
                                    AND r.ArchivedUtc IS NULL) AS AverageTimeToManifestMinutes,
                                 (SELECT COUNT(*)
                                  FROM dbo.AuditEvents ae
                                  WHERE ae.TenantId = @TenantId) AS TotalAuditEventsGenerated,
                                 (SELECT COUNT(*)
                                  FROM dbo.GovernanceApprovalRequests g
                                  INNER JOIN dbo.Runs r ON TRY_CONVERT(UNIQUEIDENTIFIER, g.RunId) = r.RunId
                                  WHERE r.TenantId = @TenantId
                                    AND g.Status = N'Approved') AS TotalGovernanceApprovalsCompleted,
                                 (SELECT MIN(gm.CreatedUtc)
                                  FROM dbo.GoldenManifests gm
                                  INNER JOIN dbo.Runs r ON r.RunId = gm.RunId
                                  WHERE r.TenantId = @TenantId
                                    AND r.ArchivedUtc IS NULL) AS FirstCommitUtc;
                             """;

        await using SqlConnection connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        MetricsRow row = await connection.QuerySingleAsync<MetricsRow>(
            new CommandDefinition(sql, new { TenantId = tenantId }, cancellationToken: cancellationToken));

        return new PilotScorecardTenantMetrics
        {
            TotalRunsCommitted = ToInt(row.TotalRunsCommitted),
            TotalManifestsCreated = ToInt(row.TotalManifestsCreated),
            TotalFindingsResolved = ToInt(row.TotalFindingsResolved),
            AverageTimeToManifestMinutes = NormalizeAvg(row.AverageTimeToManifestMinutes),
            TotalAuditEventsGenerated = ToInt(row.TotalAuditEventsGenerated),
            TotalGovernanceApprovalsCompleted = ToInt(row.TotalGovernanceApprovalsCompleted),
            FirstCommitUtc = ToOffset(row.FirstCommitUtc)
        };
    }

    private static int ToInt(object? v) => v switch
    {
        null => 0,
        int i => i,
        long l => l > int.MaxValue ? int.MaxValue : (int)l,
        _ => Convert.ToInt32(v, System.Globalization.CultureInfo.InvariantCulture)
    };

    private static double? NormalizeAvg(double? v)
    {
        if (v is null || double.IsNaN(v.Value) || double.IsInfinity(v.Value))
            return null;

        return v;
    }

    private static DateTimeOffset? ToOffset(DateTime? utc)
    {
        if (utc is null)
            return null;

        return new DateTimeOffset(DateTime.SpecifyKind(utc.Value, DateTimeKind.Utc));
    }

    private sealed class MetricsRow
    {
        public object? TotalRunsCommitted
        {
            get;
            set;
        }

        public object? TotalManifestsCreated
        {
            get;
            set;
        }

        public object? TotalFindingsResolved
        {
            get;
            set;
        }

        public double? AverageTimeToManifestMinutes
        {
            get;
            set;
        }

        public object? TotalAuditEventsGenerated
        {
            get;
            set;
        }

        public object? TotalGovernanceApprovalsCompleted
        {
            get;
            set;
        }

        public DateTime? FirstCommitUtc
        {
            get;
            set;
        }
    }
}
