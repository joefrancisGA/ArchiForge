using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace ArchiForge.Api.Health;

/// <summary>
/// Verifies the OS temp directory is writable (used for transient exports and buffering).
/// </summary>
public sealed class ProcessTempDirectoryHealthCheck : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        string tempRoot = Path.GetTempPath();
        if (string.IsNullOrWhiteSpace(tempRoot))
        {
            return Task.FromResult(
                HealthCheckResult.Unhealthy("System temp path is empty; cannot verify writable temp storage."));
        }

        string probePath = Path.Combine(tempRoot, $"archiforge-ready-{Guid.NewGuid():N}.tmp");

        try
        {
            File.WriteAllBytes(probePath, [0]);
            File.Delete(probePath);
        }
        catch (Exception ex)
        {
            return Task.FromResult(
                HealthCheckResult.Unhealthy(
                    $"Cannot write to temp directory '{tempRoot}'. Artifact/export buffering may fail.",
                    ex));
        }

        return Task.FromResult(
            HealthCheckResult.Healthy($"Temp directory is writable: {tempRoot}"));
    }
}
