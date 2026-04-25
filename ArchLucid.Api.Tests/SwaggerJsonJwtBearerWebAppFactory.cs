using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace ArchLucid.Api.Tests;

/// <summary>
///     Test host with <c>ArchLucidAuth:Mode = JwtBearer</c> so Swashbuckle emits Entra-oriented <c>securitySchemes</c>.
/// </summary>
public sealed class SwaggerJsonJwtBearerWebAppFactory : WebApplicationFactory<Program>
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
                    ["ArchLucidAuth:Mode"] = "JwtBearer",
                    ["ArchLucidAuth:Authority"] =
                        "https://login.microsoftonline.com/00000000-0000-0000-0000-000000000000/v2.0",
                    ["ArchLucidAuth:Audience"] = "api://archlucid-swagger-test"
                });
        });
    }
}
