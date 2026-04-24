using ArchLucid.Host.Composition.Startup;
using ArchLucid.Host.Core.Hosting;

using FluentAssertions;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ArchLucid.Host.Composition.Tests;

/// <summary>
///     Exercises <see cref="ServiceCollectionExtensions.AddArchLucidApplicationServices" /> for each
///     <see cref="ArchLucidHostingRole" /> so composition registration branches (worker vs API vs combined)
///     contribute to line coverage without starting Kestrel.
/// </summary>
[Trait("Suite", "Core")]
[Trait("Category", "Unit")]
public sealed class ServiceCollectionExtensionsRegistrationTests
{
    [Theory]
    [InlineData(ArchLucidHostingRole.Api)]
    [InlineData(ArchLucidHostingRole.Worker)]
    [InlineData(ArchLucidHostingRole.Combined)]
    public void AddArchLucidApplicationServices_does_not_throw_for_hosting_role(ArchLucidHostingRole role)
    {
        IConfiguration configuration = CreateCompositionTestConfiguration(role);
        ServiceCollection services = [];

        Action act = () => _ = services.AddArchLucidApplicationServices(configuration, role);

        act.Should().NotThrow();
    }

    private static IConfiguration CreateCompositionTestConfiguration(ArchLucidHostingRole role)
    {
        string roleString = role switch
        {
            ArchLucidHostingRole.Api => "Api",
            ArchLucidHostingRole.Worker => "Worker",
            _ => "Combined"
        };

        return new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["Hosting:Role"] = roleString,
                    ["ConnectionStrings:ArchLucid"] =
                        "Server=localhost;Database=ArchLucidCompositionTests;Trusted_Connection=True;TrustServerCertificate=True",
                    ["ArchLucid:StorageProvider"] = "InMemory",
                    ["AgentExecution:Mode"] = "Simulator",
                    ["AzureOpenAI:Endpoint"] = "",
                    ["AzureOpenAI:ApiKey"] = "",
                    ["AzureOpenAI:DeploymentName"] = "",
                    ["AzureOpenAI:EmbeddingDeploymentName"] = "",
                    ["RateLimiting:FixedWindow:PermitLimit"] = "100000",
                    ["RateLimiting:FixedWindow:WindowMinutes"] = "1",
                    ["RateLimiting:Expensive:PermitLimit"] = "100000",
                    ["RateLimiting:Expensive:WindowMinutes"] = "1"
                })
            .Build();
    }
}
