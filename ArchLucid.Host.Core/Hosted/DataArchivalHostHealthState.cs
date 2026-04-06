using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace ArchiForge.Host.Core.Hosted;

/// <summary>
/// Thread-safe snapshot of the last data archival hosted-service iteration outcome for readiness probes.
/// </summary>
public sealed class DataArchivalHostHealthState
{
    private readonly Lock _gate = new();

    private bool _hasAttempted;

    private bool _lastSucceeded = true;

    private DateTime _lastAttemptUtc;

    private string? _lastErrorSummary;

    /// <summary>True after at least one enabled iteration completed (success or failure).</summary>
    public bool HasAttempted
    {
        get
        {
            lock (_gate)

                return _hasAttempted;

        }
    }

    public void MarkLastIterationSucceeded()
    {
        lock (_gate)
        {
            _hasAttempted = true;
            _lastSucceeded = true;
            _lastAttemptUtc = DateTime.UtcNow;
            _lastErrorSummary = null;
        }
    }

    public void MarkLastIterationFailed(Exception ex)
    {
        ArgumentNullException.ThrowIfNull(ex);

        lock (_gate)
        {
            _hasAttempted = true;
            _lastSucceeded = false;
            _lastAttemptUtc = DateTime.UtcNow;
            _lastErrorSummary = ex.GetType().Name + ": " + ex.Message;
        }
    }

    /// <summary>
    /// When archival is enabled and the last attempt failed, returns degraded; otherwise healthy.
    /// </summary>
    public (HealthStatus Status, string Description) Evaluate(bool archivalEnabled)
    {
        lock (_gate)
        {
            if (!archivalEnabled)
                return (HealthStatus.Healthy, "Data archival is disabled.");

            if (!_hasAttempted)
                return (HealthStatus.Healthy, "Data archival enabled; no iteration has run yet.");

            if (_lastSucceeded)

                return (HealthStatus.Healthy, $"Last archival iteration succeeded at {_lastAttemptUtc:O}.");


            return (
                HealthStatus.Degraded,
                $"Last archival iteration failed at {_lastAttemptUtc:O}: {_lastErrorSummary}");
        }
    }
}
