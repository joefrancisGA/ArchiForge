using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;

namespace ArchLucid.Api.Tests;

/// <summary>Integration factory with Prometheus scrape enabled (no Basic auth) for metrics smoke tests.</summary>
public sealed class PrometheusEnabledArchLucidApiFactory : ArchLucidApiFactory
{
    /// <inheritdoc />
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Apply before base so AddArchLucidOpenTelemetry reads enabled=true during service registration.
        builder.UseSetting("Observability:Prometheus:Enabled", "true");
        builder.UseSetting("Observability:Prometheus:RequireScrapeAuthentication", "false");

        base.ConfigureWebHost(builder);

        builder.ConfigureAppConfiguration(
            (_, config) => config.AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["Observability:Prometheus:Enabled"] = "true",
                    ["Observability:Prometheus:RequireScrapeAuthentication"] = "false",
                }));
    }
}
