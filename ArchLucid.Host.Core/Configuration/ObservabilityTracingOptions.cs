namespace ArchLucid.Host.Core.Configuration;

/// <summary>
/// Trace sampling settings under <c>Observability:Tracing</c>.
/// </summary>
public sealed class ObservabilityTracingOptions
{
    /// <summary>
    /// Head-based sampling probability for root spans (0.0–1.0). Default <c>1.0</c> preserves historical AlwaysOn behavior.
    /// </summary>
    public double SamplingRatio
    {
        get;
        set;
    } = 1.0;

    /// <summary>
    /// Activity source names that should always be sampled at full fidelity. The OpenTelemetry .NET SDK does not expose
    /// <see cref="System.Diagnostics.ActivitySource"/> name in <see cref="OpenTelemetry.Trace.SamplingParameters"/> yet,
    /// so this list is reserved for documentation / future wiring; see <see cref="ArchLucid.Host.Core.Startup.ObservabilityTraceSamplingConfigurator"/>.
    /// </summary>
    public string[]? AlwaysSampleActivitySources
    {
        get;
        set;
    }
}
