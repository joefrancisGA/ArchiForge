namespace ArchLucid.Persistence.Pilots;

/// <summary>Reads auto-populated pilot scorecard metrics for one tenant (Dapper, SQL; InMemory may return zeros).</summary>
public interface IPilotScorecardMetricsReader
{
    Task<PilotScorecardTenantMetrics> GetAsync(Guid tenantId, CancellationToken cancellationToken);
}
