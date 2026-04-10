using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;

namespace ArchLucid.Api.Tests;

/// <summary>
/// API host using API-key auth so unauthenticated requests are truly anonymous (unlike DevelopmentBypass, which always authenticates).
/// </summary>
/// <remarks>
/// Base <c>appsettings.json</c> sets <c>ArchLucidAuth:Mode=DevelopmentBypass</c>; this factory overrides to API key auth.
/// For minimal-hosting / deferred factories, values must be supplied <b>before</b> and <b>after</b> <c>WebApplication.CreateBuilder</c>
/// (<see href="https://stackoverflow.com/questions/72679169/override-host-configuration-in-integration-testing-using-asp-net-core-6-minimal">SO #72679169</see>):
/// <c>UseConfiguration</c> for host bootstrap, then <c>ConfigureAppConfiguration</c> so JSON and user secrets do not win over the test profile.
/// </remarks>
public sealed class HealthEndpointSecurityApiFactory : ArchLucidApiFactory
{
    public const string IntegrationTestAdminApiKey = "health-endpoint-security-test-admin-key";

    private static Dictionary<string, string?> ApiKeyAuthConfiguration { get; } = new()
    {
        ["ArchLucidAuth:Mode"] = "ApiKey",
        ["Authentication:ApiKey:Enabled"] = "true",
        ["Authentication:ApiKey:DevelopmentBypassAll"] = "false",
        ["Authentication:ApiKey:AdminKey"] = IntegrationTestAdminApiKey
    };

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);

        IConfiguration bootstrap = new ConfigurationBuilder()
            .AddInMemoryCollection(ApiKeyAuthConfiguration)
            .Build();

        builder.UseConfiguration(bootstrap);
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(ApiKeyAuthConfiguration);
        });
    }
}
