using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace ArchLucid.Application.DataConsistency;

/// <summary>Maps the last scheduled reconciliation outcome to ASP.NET health status.</summary>
public sealed class DataConsistencyHealthCheck(DataConsistencyReconciliationHealthState healthState) : IHealthCheck
{
    private readonly DataConsistencyReconciliationHealthState _healthState =
        healthState ?? throw new ArgumentNullException(nameof(healthState));

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        _healthState.TrySnapshot(out bool hasRun, out DataConsistencyReport? report, out string? error);

        if (!hasRun)
            return Task.FromResult(HealthCheckResult.Unhealthy("Data consistency reconciliation has not run yet."));

        if (error is not null)
            return Task.FromResult(
                HealthCheckResult.Unhealthy("Data consistency reconciliation failed: " + error));

        if (report is null)
            return Task.FromResult(HealthCheckResult.Unhealthy("Data consistency reconciliation state is inconsistent (no report)."));

        if (report.Findings.Any(f => f.Severity == DataConsistencyFindingSeverity.Critical))
            return Task.FromResult(
                HealthCheckResult.Unhealthy("Critical data consistency findings detected in the last reconciliation."));

        return Task.FromResult(report.Findings.Any(f => f.Severity == DataConsistencyFindingSeverity.Warning) ? HealthCheckResult.Degraded("Warning-level data consistency findings detected in the last reconciliation.") : HealthCheckResult.Healthy("Last data consistency reconciliation reported no warnings or critical issues."));
    }
}
