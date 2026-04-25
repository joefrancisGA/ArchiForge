using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;

namespace ArchLucid.Api.Tests.Security;

/// <summary>
///     Hosts the API in <c>ApiKey</c> auth mode with a distinct <b>read-only</b> key and <b>admin</b> key so authorization
///     policy boundaries can be exercised without the DevelopmentBypass default principal.
/// </summary>
public sealed class ApiKeyReaderAndAdminArchLucidApiFactory : ArchLucidApiFactory
{
    // Long random-looking strings: avoid placeholder detection in ApiKeyAuthenticationOptions.
    public const string IntegrationTestAdminApiKey = "api-key-bdr-admin-aB3xK9mN2pQ7wR5vZ1yC8dE6fG0hH4jJ";
    public const string IntegrationTestReaderApiKey = "api-key-bdr-reader-bC4xL0nO3pR8sT6uV2wW5xX7yY9zZ1aA";

    private static Dictionary<string, string?> KeyModeConfiguration
    {
        get;
    } = new()
    {
        ["ArchLucidAuth:Mode"] = "ApiKey",
        ["Authentication:ApiKey:Enabled"] = "true",
        ["Authentication:ApiKey:DevelopmentBypassAll"] = "false",
        ["Authentication:ApiKey:AdminKey"] = IntegrationTestAdminApiKey,
        ["Authentication:ApiKey:ReadOnlyKey"] = IntegrationTestReaderApiKey
    };

    /// <inheritdoc />
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        IConfiguration bootstrap = new ConfigurationBuilder().AddInMemoryCollection(KeyModeConfiguration).Build();
        builder.UseConfiguration(bootstrap);
        builder.ConfigureAppConfiguration((_, config) => { config.AddInMemoryCollection(KeyModeConfiguration); });
    }
}
