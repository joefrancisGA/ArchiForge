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
                if (consoleExporterEnabled)
                {
                    tracing.AddConsoleExporter();
                }
            })
            .WithMetrics(metrics =>
            {
                metrics.AddAspNetCoreInstrumentation();
                metrics.AddHttpClientInstrumentation();
                if (prometheusEnabled)
                {
                    metrics.AddPrometheusExporter();
                }
            });

        return services;
    }
}
