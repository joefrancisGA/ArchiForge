using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace ArchLucid.Api.Tests;

/// <summary>
///     Minimal API host for OpenAPI contract checks: in-memory authority storage, no SQL, Development pipeline
///     (Scalar + <c>/swagger/v1/swagger.json</c> + Microsoft OpenAPI; generation uses <c>CustomSchemaIds</c> and optional
///     auth security filters).
/// </summary>
public class OpenApiContractWebAppFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["ArchLucid:StorageProvider"] = "InMemory",
                    ["ConnectionStrings:ArchLucid"] = "",
                    ["AgentExecution:Mode"] = "Simulator",
                    ["AzureOpenAI:Endpoint"] = "",
                    ["AzureOpenAI:ApiKey"] = "",
                    ["AzureOpenAI:DeploymentName"] = "",
                    ["AzureOpenAI:EmbeddingDeploymentName"] = "",
                    ["RateLimiting:FixedWindow:PermitLimit"] = "100000",
                    ["RateLimiting:FixedWindow:WindowMinutes"] = "1",
                    ["RateLimiting:Expensive:PermitLimit"] = "100000",
                    ["RateLimiting:Expensive:WindowMinutes"] = "1",
                    ["RateLimiting:Replay:Light:PermitLimit"] = "100000",
                    ["RateLimiting:Replay:Heavy:PermitLimit"] = "100000",
                    ["RateLimiting:Registration:PermitLimit"] = "100000",
                    ["RateLimiting:Registration:WindowMinutes"] = "1",
                    ["Billing:Provider"] = "Noop"
                });
        });
    }
}
