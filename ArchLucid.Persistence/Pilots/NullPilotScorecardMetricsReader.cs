namespace ArchLucid.Persistence.Pilots;

/// <summary>In-memory / non-SQL storage: no relational aggregates; returns empty metrics (operator UI still loads).</summary>
public sealed class NullPilotScorecardMetricsReader : IPilotScorecardMetricsReader
{
    public Task<PilotScorecardTenantMetrics> GetAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        _ = tenantId;
        _ = cancellationToken;

        return Task.FromResult(
            new PilotScorecardTenantMetrics
            {
                TotalRunsCommitted = 0,
                TotalManifestsCreated = 0,
                TotalFindingsResolved = 0,
                AverageTimeToManifestMinutes = null,
                TotalAuditEventsGenerated = 0,
                TotalGovernanceApprovalsCompleted = 0,
                FirstCommitUtc = null
            });
    }
}
