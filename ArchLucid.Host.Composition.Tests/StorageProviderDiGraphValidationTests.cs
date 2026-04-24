using ArchLucid.Core.Scoping;
using ArchLucid.Host.Composition.Configuration;
using ArchLucid.Host.Composition.Startup;
using ArchLucid.Host.Core.Hosting;
using ArchLucid.Persistence.Connections;

using FluentAssertions;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace ArchLucid.Host.Composition.Tests;

/// <summary>
///     Validates that the composed DI graph can be built with <see cref="ServiceProviderOptions.ValidateOnBuild" /> for
///     each
///     storage provider — catches missing registrations (e.g. <c>IArtifactBlobStore</c> on the InMemory path).
/// </summary>
[Trait("Suite", "Core")]
[Trait("Category", "Unit")]
public sealed class StorageProviderDiGraphValidationTests
{
    [Fact]
    public void InMemory_storage_full_composition_validates_on_build()
    {
        IConfiguration configuration = CreateOpenApiLikeInMemoryConfiguration();
        ServiceCollection services = CreateCompositionServices(configuration);
        services.AddHttpContextAccessor();
        _ = services.AddArchLucidApplicationServices(configuration, ArchLucidHostingRole.Api);

        ServiceProviderOptions options = new() { ValidateOnBuild = true, ValidateScopes = true };
        Action act = () =>
        {
            ServiceProvider provider = services.BuildServiceProvider(options);
            provider.Dispose();
        };

        act.Should().NotThrow();
    }

    /// <summary>
    ///     Sql + <see cref="ServiceProviderOptions.ValidateOnBuild" /> is not run here: building the full graph eagerly
    ///     constructs many scoped services and can block on SQL/network; that path is covered by API integration tests
    ///     and CI full regression. This test only asserts Sql storage registration completes and wires the connection stack.
    /// </summary>
    [Fact]
    public void Sql_storage_AddArchLucidStorage_registers_sql_connection_abstractions()
    {
        IConfiguration configuration = CreateSqlCompositionConfiguration();
        ServiceCollection services = [];
        services.AddSingleton(typeof(IConfiguration), configuration);
        services.AddLogging();
        _ = services.AddArchLucidStorage(configuration);

        List<ServiceDescriptor> list = services.ToList();
        list.Should().Contain(d => d.ServiceType == typeof(ISqlConnectionFactory));
        list.Should().Contain(d => d.ServiceType == typeof(SqlConnectionFactory));
    }

    private static ServiceCollection CreateCompositionServices(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        ServiceCollection services = [];
        services.AddSingleton(typeof(IConfiguration), configuration);
        services.AddSingleton<IHostEnvironment>(new CompositionTestHostEnvironment(Environments.Development));
        services.AddLogging();
        services.AddSingleton<IScopeContextProvider, FixedCompositionScopeContextProvider>();

        return services;
    }

    private static IConfiguration CreateOpenApiLikeInMemoryConfiguration()
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["Hosting:Role"] = "Api",
                    ["ArchLucid:StorageProvider"] = "InMemory",
                    ["AgentExecution:Mode"] = "Simulator",
                    ["AzureOpenAI:Endpoint"] = "",
                    ["AzureOpenAI:ApiKey"] = "",
                    ["AzureOpenAI:DeploymentName"] = "",
                    ["AzureOpenAI:EmbeddingDeploymentName"] = "",
                    ["FeatureManagement:FeatureFlags:AsyncAuthorityPipeline"] = "false",
                    ["RateLimiting:FixedWindow:PermitLimit"] = "100000",
                    ["RateLimiting:FixedWindow:WindowMinutes"] = "1",
                    ["RateLimiting:Expensive:PermitLimit"] = "100000",
                    ["RateLimiting:Expensive:WindowMinutes"] = "1",
                    ["RateLimiting:Replay:Light:PermitLimit"] = "100000",
                    ["RateLimiting:Replay:Heavy:PermitLimit"] = "100000",
                    ["LlmCompletionCache:Enabled"] = "false",
                    ["HotPathCache:Enabled"] = "false"
                })
            .Build();
    }

    private static IConfiguration CreateSqlCompositionConfiguration()
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["Hosting:Role"] = "Api",
                    ["ArchLucid:StorageProvider"] = "Sql",
                    ["ConnectionStrings:ArchLucid"] =
                        "Server=.;Database=ArchLucidDiGraphTests;Trusted_Connection=True;TrustServerCertificate=True",
                    ["AgentExecution:Mode"] = "Simulator",
                    ["AzureOpenAI:Endpoint"] = "",
                    ["AzureOpenAI:ApiKey"] = "",
                    ["AzureOpenAI:DeploymentName"] = "",
                    ["AzureOpenAI:EmbeddingDeploymentName"] = "",
                    ["FeatureManagement:FeatureFlags:AsyncAuthorityPipeline"] = "false",
                    ["RateLimiting:FixedWindow:PermitLimit"] = "100000",
                    ["RateLimiting:FixedWindow:WindowMinutes"] = "1",
                    ["RateLimiting:Expensive:PermitLimit"] = "100000",
                    ["RateLimiting:Expensive:WindowMinutes"] = "1",
                    ["LlmCompletionCache:Enabled"] = "false",
                    ["HotPathCache:Enabled"] = "false"
                })
            .Build();
    }

    private sealed class FixedCompositionScopeContextProvider : IScopeContextProvider
    {
        public ScopeContext GetCurrentScope()
        {
            return new ScopeContext
            {
                TenantId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                WorkspaceId = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                ProjectId = Guid.Parse("33333333-3333-3333-3333-333333333333")
            };
        }
    }
}
