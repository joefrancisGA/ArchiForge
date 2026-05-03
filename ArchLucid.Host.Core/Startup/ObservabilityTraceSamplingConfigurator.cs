using System.Globalization;

using ArchLucid.Host.Core.Configuration;

using OpenTelemetry.Trace;

namespace ArchLucid.Host.Core.Startup;

/// <summary>
/// Applies head-based trace sampling from <c>Observability:Tracing</c> before any trace instrumentations are registered.
/// </summary>
public static class ObservabilityTraceSamplingConfigurator
{
    /// <summary>
    /// Configures the trace <see cref="Sampler"/> on <paramref name="tracing"/> from configuration.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <c>Observability:Tracing:AlwaysSampleActivitySources</c> is read for operator documentation and options binding,
    /// but per-source overrides are not implemented in-process: OpenTelemetry .NET does not pass ActivitySource name into
    /// <see cref="Sampler.ShouldSample(OpenTelemetry.Trace.SamplingParameters@)"/> (see
    /// https://github.com/open-telemetry/opentelemetry-dotnet/issues/4752). Use an OTLP collector tail-sampling policy
    /// (or similar) to keep high-value sources such as <c>ArchLucid.AuthorityRun</c> at full fidelity in production.
    /// </para>
    /// </remarks>
    public static void ConfigureTraceSampling(TracerProviderBuilder tracing, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(tracing);
        ArgumentNullException.ThrowIfNull(configuration);

        string[]? alwaysSampleActivitySources = configuration
            .GetSection($"{ObservabilityHostOptions.SectionName}:Tracing:AlwaysSampleActivitySources")
            .Get<string[]>();

        if (alwaysSampleActivitySources is { Length: > 0 })
        {
            // TODO: Honor AlwaysSampleActivitySources in-process once OpenTelemetry .NET exposes ActivitySource (or
            // equivalent) on SamplingParameters, or introduce a supported hook (see issue #4752 above). Until then,
            // operators should enforce per-source retention via OTLP collector tail sampling or backend rules.
        }

        // Avoid ConfigurationBinder.GetValue<double> when the key exists but is not parseable — it throws and would
        // fail host startup on a typo in production config.
        string? samplingRatioRaw = configuration[$"{ObservabilityHostOptions.SectionName}:Tracing:SamplingRatio"]?.Trim();
        double samplingRatio = 1.0;

        if (!string.IsNullOrEmpty(samplingRatioRaw))

            if (!double.TryParse(
                    samplingRatioRaw,
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture,
                    out double parsed) ||
                double.IsNaN(parsed) ||
                double.IsInfinity(parsed))

                samplingRatio = 1.0;

            else

                samplingRatio = Math.Clamp(parsed, 0.0, 1.0);

        if (samplingRatio < 1.0)

            tracing.SetSampler(new ParentBasedSampler(new TraceIdRatioBasedSampler(samplingRatio)));
    }
}
