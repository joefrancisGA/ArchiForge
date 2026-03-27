using ArchiForge.Core.Diagnostics;
using ArchiForge.DecisionEngine.Validation;

using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace ArchiForge.Api.Startup;

internal static class ObservabilityExtensions
{
    public static IServiceCollection AddArchiForgeOpenTelemetry(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        bool prometheusEnabled = configuration.GetValue("Observability:Prometheus:Enabled", false);
        bool consoleExporterEnabled = configuration.GetValue("Observability:ConsoleExporter:Enabled", environment.IsDevelopment());

        string version = typeof(ObservabilityExtensions).Assembly.GetName().Version?.ToString() ?? "unknown";

        services.AddOpenTelemetry()
            .ConfigureResource(resource => resource
                .AddService(
                    serviceName: "ArchiForge.Api",
                    serviceVersion: version,
                    serviceInstanceId: Environment.MachineName))
            .WithTracing(tracing =>
            {
                tracing.AddAspNetCoreInstrumentation();
                tracing.AddHttpClientInstrumentation();
                tracing.AddSqlClientInstrumentation();
                tracing.AddSource(
                    ArchiForgeInstrumentation.AdvisoryScan.Name,
                    ArchiForgeInstrumentation.AuthorityRun.Name,
                    ArchiForgeInstrumentation.RetrievalIndex.Name);

                if (consoleExporterEnabled)
                {
                    tracing.AddConsoleExporter();
                }
            })
            .WithMetrics(metrics =>
            {
                metrics.AddAspNetCoreInstrumentation();
                metrics.AddHttpClientInstrumentation();

                // Wires the schema-validation meter so Prometheus / OTLP exporters receive
                // schema_validation_total and schema_validation_duration_ms.
                metrics.AddMeter(SchemaValidationService.MeterName);
                metrics.AddMeter(ArchiForgeInstrumentation.MeterName);

                if (prometheusEnabled)
                {
                    metrics.AddPrometheusExporter();
                }
            });

        return services;
    }
}
