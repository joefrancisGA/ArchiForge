using ArchiForge.Host.Core.Hosted;
using ArchiForge.Persistence.Archival;

using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace ArchiForge.Host.Core.Health;

/// <summary>
/// Readiness signal when <see cref="DataArchivalOptions.Enabled"/> and the last archival iteration failed.
/// </summary>
public sealed class DataArchivalHostHealthCheck(
    DataArchivalHostHealthState healthState,
    IOptionsMonitor<DataArchivalOptions> optionsMonitor) : IHealthCheck
{
    private readonly DataArchivalHostHealthState _healthState =
        healthState ?? throw new ArgumentNullException(nameof(healthState));

    private readonly IOptionsMonitor<DataArchivalOptions> _optionsMonitor =
        optionsMonitor ?? throw new ArgumentNullException(nameof(optionsMonitor));

    /// <inheritdoc />
    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        DataArchivalOptions opts = _optionsMonitor.CurrentValue;
        (HealthStatus status, string description) = _healthState.Evaluate(opts.Enabled);

        return Task.FromResult(
            status == HealthStatus.Healthy
                ? HealthCheckResult.Healthy(description)
                : HealthCheckResult.Degraded(description));
    }
}
