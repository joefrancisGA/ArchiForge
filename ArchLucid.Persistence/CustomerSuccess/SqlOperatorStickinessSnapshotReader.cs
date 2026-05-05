using System.Diagnostics.CodeAnalysis;

using ArchLucid.Core.Audit;
using ArchLucid.Core.CustomerSuccess;
using ArchLucid.Core.Scoping;
using ArchLucid.Persistence.Connections;

using Dapper;

using Microsoft.Data.SqlClient;

namespace ArchLucid.Persistence.CustomerSuccess;

[ExcludeFromCodeCoverage(Justification = "SQL Server–dependent reader.")]
public sealed class SqlOperatorStickinessSnapshotReader(
    ISqlConnectionFactory connectionFactory,
    IRlsSessionContextApplicator rlsSessionContextApplicator) : IOperatorStickinessSnapshotReader
{
    private readonly ISqlConnectionFactory _connectionFactory =
        connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));

    private readonly IRlsSessionContextApplicator _rlsSessionContextApplicator =
        rlsSessionContextApplicator ?? throw new ArgumentNullException(nameof(rlsSessionContextApplicator));

    public async Task<OperatorStickinessSignals> GetOperatorSignalsAsync(
        Guid tenantId,
        Guid workspaceId,
        Guid projectId,
        CancellationToken cancellationToken)
    {
        const string sql = """
                           SELECT
                               (SELECT COUNT(*)
                                FROM dbo.Runs r
                                WHERE r.TenantId = @TenantId
                                  AND r.WorkspaceId = @WorkspaceId
                                  AND r.ScopeProjectId = @ProjectId
                                  AND r.ArchivedUtc IS NULL) AS TotalRuns,
                               (SELECT COUNT(*)
                                FROM dbo.Runs r
                                WHERE r.TenantId = @TenantId
                                  AND r.WorkspaceId = @WorkspaceId
                                  AND r.ScopeProjectId = @ProjectId
                                  AND r.ArchivedUtc IS NULL
                                  AND (
                                      NULLIF(LTRIM(RTRIM(r.CurrentManifestVersion)), N'') IS NOT NULL
                                      OR r.GoldenManifestId IS NOT NULL)) AS CommittedRuns,
                               (SELECT TOP (1) r.RunId
                                FROM dbo.Runs r
                                WHERE r.TenantId = @TenantId
                                  AND r.WorkspaceId = @WorkspaceId
                                  AND r.ScopeProjectId = @ProjectId
                                  AND r.ArchivedUtc IS NULL
                                ORDER BY r.CreatedUtc DESC) AS LatestRunId,
                               (SELECT COUNT_BIG(1)
                                FROM dbo.AuditEvents ae
                                WHERE ae.TenantId = @TenantId
                                  AND ae.WorkspaceId = @WorkspaceId
                                  AND ae.ProjectId = @ProjectId
                                  AND ae.OccurredUtc >= DATEADD(DAY, -30, SYSUTCDATETIME())
                                  AND ae.EventType = @Comparison) AS Comparisons30d,
                               (SELECT COUNT_BIG(1)
                                FROM dbo.GovernanceApprovalRequests g
                                WHERE g.TenantId = @TenantId
                                  AND g.WorkspaceId = @WorkspaceId
                                  AND g.ProjectId = @ProjectId
                                  AND g.Status NOT IN (N'Approved', N'Rejected')) AS GovPending;
                           """;

        await using SqlConnection connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        await _rlsSessionContextApplicator.ApplyAsync(connection, cancellationToken);

        OperatorSignalsRow row = await connection.QuerySingleAsync<OperatorSignalsRow>(
            new CommandDefinition(
                sql,
                new
                {
                    TenantId = tenantId,
                    WorkspaceId = workspaceId,
                    ProjectId = projectId,
                    Comparison = AuditEventTypes.ComparisonSummaryPersisted
                },
                cancellationToken: cancellationToken));

        return new OperatorStickinessSignals(
            ToInt(row.TotalRuns),
            ToInt(row.CommittedRuns),
            row.LatestRunId,
            ToInt(row.Comparisons30d),
            ToInt(row.GovPending));
    }

    public async Task<PilotFunnelSnapshot> GetFunnelSnapshotAsync(
        Guid tenantId,
        Guid workspaceId,
        Guid projectId,
        CancellationToken cancellationToken)
    {
        const string sql = """
                           SELECT
                               (SELECT MIN(r.CreatedUtc)
                                FROM dbo.Runs r
                                WHERE r.TenantId = @TenantId
                                  AND r.WorkspaceId = @WorkspaceId
                                  AND r.ScopeProjectId = @ProjectId
                                  AND r.ArchivedUtc IS NULL) AS FirstRunUtc,
                               (SELECT MIN(gm.CreatedUtc)
                                FROM dbo.GoldenManifests gm
                                INNER JOIN dbo.Runs r ON r.RunId = gm.RunId
                                WHERE r.TenantId = @TenantId
                                  AND r.WorkspaceId = @WorkspaceId
                                  AND r.ScopeProjectId = @ProjectId
                                  AND r.ArchivedUtc IS NULL) AS FirstManifestUtc,
                               (SELECT MIN(ae.OccurredUtc)
                                FROM dbo.AuditEvents ae
                                WHERE ae.TenantId = @TenantId
                                  AND ae.WorkspaceId = @WorkspaceId
                                  AND ae.ProjectId = @ProjectId
                                  AND ae.EventType = @Comparison) AS FirstComparisonUtc,
                               (SELECT MIN(ae.OccurredUtc)
                                FROM dbo.AuditEvents ae
                                WHERE ae.TenantId = @TenantId
                                  AND ae.WorkspaceId = @WorkspaceId
                                  AND ae.ProjectId = @ProjectId
                                  AND ae.EventType IN (@ArtDl, @BundleDl)) AS FirstDownloadUtc,
                               (SELECT MIN(ae.OccurredUtc)
                                FROM dbo.AuditEvents ae
                                WHERE ae.TenantId = @TenantId
                                  AND ae.WorkspaceId = @WorkspaceId
                                  AND ae.ProjectId = @ProjectId
                                  AND ae.EventType = @Replay) AS FirstReplayUtc,
                               (SELECT COUNT(*)
                                FROM dbo.Runs r
                                WHERE r.TenantId = @TenantId
                                  AND r.WorkspaceId = @WorkspaceId
                                  AND r.ScopeProjectId = @ProjectId
                                  AND r.ArchivedUtc IS NULL) AS TotalRuns,
                               (SELECT COUNT(*)
                                FROM dbo.Runs r
                                WHERE r.TenantId = @TenantId
                                  AND r.WorkspaceId = @WorkspaceId
                                  AND r.ScopeProjectId = @ProjectId
                                  AND r.ArchivedUtc IS NULL
                                  AND (
                                      NULLIF(LTRIM(RTRIM(r.CurrentManifestVersion)), N'') IS NOT NULL
                                      OR r.GoldenManifestId IS NOT NULL)) AS CommittedRuns,
                               (SELECT COUNT_BIG(1)
                                FROM dbo.ProductLearningPilotSignals s
                                WHERE s.TenantId = @TenantId
                                  AND s.WorkspaceId = @WorkspaceId
                                  AND s.ProjectId = @ProjectId
                                  AND s.RecordedUtc >= DATEADD(DAY, -90, SYSUTCDATETIME())) AS PlSignals90d;
                           """;

        await using SqlConnection connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        await _rlsSessionContextApplicator.ApplyAsync(connection, cancellationToken);

        FunnelRow row = await connection.QuerySingleAsync<FunnelRow>(
            new CommandDefinition(
                sql,
                new
                {
                    TenantId = tenantId,
                    WorkspaceId = workspaceId,
                    ProjectId = projectId,
                    Comparison = AuditEventTypes.ComparisonSummaryPersisted,
                    ArtDl = AuditEventTypes.ArtifactDownloaded,
                    BundleDl = AuditEventTypes.BundleDownloaded,
                    Replay = AuditEventTypes.ReplayExecuted
                },
                cancellationToken: cancellationToken));

        return new PilotFunnelSnapshot(
            ToNullableUtcDateTime(row.FirstRunUtc),
            ToNullableUtcDateTime(row.FirstManifestUtc),
            ToNullableUtcDateTime(row.FirstComparisonUtc),
            ToNullableUtcDateTime(row.FirstDownloadUtc),
            ToNullableUtcDateTime(row.FirstReplayUtc),
            ToInt(row.TotalRuns),
            ToInt(row.CommittedRuns),
            ToInt(row.PlSignals90d));
    }

    // COUNT_BIG returns bigint; cap to int range for domain model compatibility.
    private static int ToInt(long v) => v > int.MaxValue ? int.MaxValue : (int)v;

    private static DateTime? ToNullableUtcDateTime(DateTime? utc)
    {
        if (utc is null)
            return null;

        return DateTime.SpecifyKind(utc.Value, DateTimeKind.Utc);
    }

    private sealed class OperatorSignalsRow
    {
        public long TotalRuns { get; init; }
        public long CommittedRuns { get; init; }
        public Guid? LatestRunId { get; init; }
        public long Comparisons30d { get; init; }
        public long GovPending { get; init; }
    }

    private sealed class FunnelRow
    {
        public DateTime? FirstRunUtc { get; init; }
        public DateTime? FirstManifestUtc { get; init; }
        public DateTime? FirstComparisonUtc { get; init; }
        public DateTime? FirstDownloadUtc { get; init; }
        public DateTime? FirstReplayUtc { get; init; }
        public long TotalRuns { get; init; }
        public long CommittedRuns { get; init; }
        public long PlSignals90d { get; init; }
    }
}
