using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace ArchiForge.Api.Health;

/// <summary>Verifies the default compliance rule pack file exists next to the running API (same path as FileComplianceRulePackLoader).</summary>
public sealed class ComplianceRulePackHealthCheck : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        string fullPath = Path.Combine(AppContext.BaseDirectory, EmbeddedContentPaths.ComplianceRulePackRelativePath);

        if (!File.Exists(fullPath))
        {
            return Task.FromResult(
                HealthCheckResult.Unhealthy(
                    $"Compliance rule pack not found at '{fullPath}'. Expected bundled content from ArchiForge.Decisioning (CopyToOutputDirectory)."));
        }

        return Task.FromResult(
            HealthCheckResult.Healthy($"Compliance rule pack present: {EmbeddedContentPaths.ComplianceRulePackRelativePath}."));
    }
}
