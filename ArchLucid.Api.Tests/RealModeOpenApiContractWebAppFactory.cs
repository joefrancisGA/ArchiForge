using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace ArchLucid.Api.Tests;

/// <summary>
///     <see cref="OpenApiContractWebAppFactory" /> with <c>AgentExecution:Mode=Real</c> and stub Azure OpenAI keys for
///     cost-preview integration tests.
/// </summary>
public sealed class RealModeOpenApiContractWebAppFactory : OpenApiContractWebAppFactory
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.ConfigureAppConfiguration(
            (_, config) => config.AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["AgentExecution:Mode"] = "Real",
                    ["AzureOpenAI:Endpoint"] = "https://integration-test.openai.azure.com/",
                    ["AzureOpenAI:ApiKey"] = "test-key-not-real",
                    ["AzureOpenAI:DeploymentName"] = "gpt-test-deploy",
                    ["AzureOpenAI:MaxCompletionTokens"] = "1024"
                }));
    }
}
