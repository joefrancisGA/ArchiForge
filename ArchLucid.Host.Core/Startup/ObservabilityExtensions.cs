using ArchLucid.Core.Diagnostics;
using ArchLucid.Decisioning.Validation;
using ArchLucid.Host.Core.Configuration;

using Azure.Monitor.OpenTelemetry.Exporter;

using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace ArchLucid.Host.Core.Startup;

public static class ObservabilityExtensions
{
    /// <summary>
    /// Registers OpenTelemetry tracing and metrics for ArchLucid hosts.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>Trace sampling:</strong> <c>Observability:Tracing:SamplingRatio</c> (default <c>1.0</c>) enables head-based
    /// <see cref="OpenTelemetry.Trace.TraceIdRatioBasedSampler"/> for root spans when below <c>1.0</c>, wrapped in
    /// <see cref="OpenTelemetry.Trace.ParentBasedSampler"/> so remote parent decisions are respected. Unparseable values
    /// fall back to <c>1.0</c> so a typo does not fail startup.
    /// Optional <c>Observability:Tracing:AlwaysSampleActivitySources</c> is bound for future use; per-source always-on
    /// sampling in-process is not available until the SDK exposes source name on sampling parameters — use collector
    /// tail sampling for high-value sources (see <c>ObservabilityTraceSamplingConfigurator</c>).
    /// </para>
    /// <para>
    /// <strong>OTLP:</strong> When <c>Observability:Otlp:Endpoint</c> is non-empty, trace and metric OTLP exporters
    /// are registered by default. Set <c>Observability:Otlp:Enabled</c> to <c>false</c> to force OTLP off even if
    /// an endpoint string is present (kill-switch). When the endpoint is empty, OTLP is always off.
    /// </para>
    /// <para>
    /// <strong>Azure Monitor / Application Insights:</strong> When <c>APPLICATIONINSIGHTS_CONNECTION_STRING</c>,
    /// <c>ApplicationInsights:ConnectionString</c>, or <c>Observability:AzureMonitor:ApplicationInsightsConnectionString</c>
    /// is set, trace and metric exporters are registered <b>in addition</b> to
    /// OTLP / console (dual export). Prefer private ingestion paths in regulated environments.
    /// </para>
    /// </remarks>
    public static IServiceCollection AddArchLucidOpenTelemetry(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment,
        string telemetryServiceName)
    {
        bool consoleExporterEnabled = configuration.GetValue("Observability:ConsoleExporter:Enabled", environment.IsDevelopment());

        string? otlpEndpointRaw = configuration["Observability:Otlp:Endpoint"]?.Trim();
        bool endpointConfigured = !string.IsNullOrWhiteSpace(otlpEndpointRaw);
        bool? otlpEnabledOverride = configuration.GetValue<bool?>("Observability:Otlp:Enabled");

        bool useOtlp = endpointConfigured && (!otlpEnabledOverride.HasValue || otlpEnabledOverride.Value);

        Uri? otlpEndpointUri = null;
        OtlpExportProtocol otlpProtocol = OtlpExportProtocol.Grpc;
        string? otlpHeaders = configuration["Observability:Otlp:Headers"]?.Trim();

        if (useOtlp)
        {
            otlpEndpointUri = new Uri(otlpEndpointRaw!, UriKind.Absolute);
            string protocolStr = configuration["Observability:Otlp:Protocol"]?.Trim() ?? "Grpc";
            otlpProtocol = string.Equals(protocolStr, "HttpProtobuf", StringComparison.OrdinalIgnoreCase)
                ? OtlpExportProtocol.HttpProtobuf
                : OtlpExportProtocol.Grpc;
        }

        string? applicationInsightsConnectionString = configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"]?.Trim();

        if (string.IsNullOrWhiteSpace(applicationInsightsConnectionString))
            applicationInsightsConnectionString = configuration["ApplicationInsights:ConnectionString"]?.Trim();

        if (string.IsNullOrWhiteSpace(applicationInsightsConnectionString))
            applicationInsightsConnectionString =
                configuration["Observability:AzureMonitor:ApplicationInsightsConnectionString"]?.Trim();

        bool useAzureMonitorExporter = !string.IsNullOrWhiteSpace(applicationInsightsConnectionString);

        BuildProvenance build = BuildProvenance.FromAssembly(typeof(ObservabilityExtensions).Assembly);

        ArchLucidInstrumentation.EnsureOutboxDepthObservableGaugesRegistered();
        ArchLucidInstrumentation.EnsureTrialFunnelObservableGaugesRegistered();
        ArchLucidInstrumentation.EnsureCircuitBreakerStateObservableGaugesRegistered();

        services.Configure<ObservabilityHostOptions>(
            configuration.GetSection(ObservabilityHostOptions.SectionName));

        services.AddOpenTelemetry()
            .ConfigureResource(resource => resource
                .AddService(
                    serviceName: telemetryServiceName,
                    serviceVersion: build.InformationalVersion,
                    serviceInstanceId: Environment.MachineName))
            .WithTracing(tracing =>
            {
                ObservabilityTraceSamplingConfigurator.ConfigureTraceSampling(tracing, configuration);

                tracing.AddAspNetCoreInstrumentation();
                tracing.AddHttpClientInstrumentation();
                tracing.AddSqlClientInstrumentation();
                tracing.AddSource(
                    ArchLucidInstrumentation.AdvisoryScan.Name,
                    ArchLucidInstrumentation.AuthorityRun.Name,
                    ArchLucidInstrumentation.RetrievalIndex.Name,
                    ArchLucidInstrumentation.AgentHandler.Name,
                    ArchLucidInstrumentation.AgentLlmCompletion.Name,
                    ArchLucidInstrumentation.RetrievalIndexingOutbox.Name,
                    ArchLucidInstrumentation.IntegrationEventOutbox.Name,
                    ArchLucidInstrumentation.DataArchival.Name);

                if (consoleExporterEnabled)
                    tracing.AddConsoleExporter();

                if (otlpEndpointUri is not null)

                    tracing.AddOtlpExporter(o =>
                    {
                        o.Endpoint = otlpEndpointUri;
                        o.Protocol = otlpProtocol;

                        if (!string.IsNullOrEmpty(otlpHeaders))
                            o.Headers = otlpHeaders;
                    });

                if (useAzureMonitorExporter)

                    tracing.AddAzureMonitorTraceExporter(o =>
                        o.ConnectionString = applicationInsightsConnectionString);
            })
            .WithMetrics(metrics =>
            {
                metrics.AddAspNetCoreInstrumentation();
                metrics.AddHttpClientInstrumentation();

                // Wires the schema-validation meter so Prometheus / OTLP exporters receive
                // schema_validation_total and schema_validation_duration_ms.
                metrics.AddMeter(SchemaValidationService.MeterName);
                metrics.AddMeter(ArchLucidInstrumentation.MeterName);

                // Always register the Prometheus exporter on the shared MeterProvider so
                // UseOpenTelemetryPrometheusScrapingEndpoint can resolve it whenever scrape is enabled
                // (integration tests flip Prometheus on after the host builder merges configuration).
                metrics.AddPrometheusExporter();

                if (otlpEndpointUri is not null)

                    metrics.AddOtlpExporter(o =>
                    {
                        o.Endpoint = otlpEndpointUri;
                        o.Protocol = otlpProtocol;

                        if (!string.IsNullOrEmpty(otlpHeaders))
                            o.Headers = otlpHeaders;
                    });

                if (useAzureMonitorExporter)

                    metrics.AddAzureMonitorMetricExporter(o =>
                        o.ConnectionString = applicationInsightsConnectionString);
            });

        return services;
    }
}
