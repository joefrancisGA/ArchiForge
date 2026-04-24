using ArchLucid.Core.Scoping;
using ArchLucid.Host.Composition.Startup;
using ArchLucid.Host.Core.Hosted;
using ArchLucid.Host.Core.Hosting;
using ArchLucid.Host.Core.Jobs;

using FluentAssertions;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ArchLucid.Host.Composition.Tests;

[Trait("Suite", "Core")]
[Trait("Category", "Unit")]
public sealed class ContainerJobsOffloadRegistrationTests
{
    [Fact]
    public void
        AddArchLucidApplicationServices_Worker_offloads_advisory_scan_does_not_register_AdvisoryScanHostedService()
    {
        Dictionary<string, string?> data = CreateWorkerCompositionDictionary();
        data["Jobs:OffloadedToContainerJobs:0"] = ArchLucidJobNames.AdvisoryScan;

        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(data).Build();
        ServiceCollection services = CreateCoreServices(configuration);

        _ = services.AddArchLucidApplicationServices(configuration, ArchLucidHostingRole.Worker);

        bool hasHosted = services.Any(static d =>
            d.ServiceType == typeof(IHostedService)
            && d.ImplementationType == typeof(AdvisoryScanHostedService));

        hasHosted.Should().BeFalse();
    }

    [Fact]
    public void AddArchLucidApplicationServices_Worker_default_registers_AdvisoryScanHostedService()
    {
        Dictionary<string, string?> data = CreateWorkerCompositionDictionary();

        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(data).Build();
        ServiceCollection services = CreateCoreServices(configuration);

        _ = services.AddArchLucidApplicationServices(configuration, ArchLucidHostingRole.Worker);

        bool hasHosted = services.Any(static d =>
            d.ServiceType == typeof(IHostedService)
            && d.ImplementationType == typeof(AdvisoryScanHostedService));

        hasHosted.Should().BeTrue();
    }

    [Fact]
    public void AddArchLucidApplicationServices_Worker_registers_TenantHealthScoringHostedService()
    {
        Dictionary<string, string?> data = CreateWorkerCompositionDictionary();

        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(data).Build();
        ServiceCollection services = CreateCoreServices(configuration);

        _ = services.AddArchLucidApplicationServices(configuration, ArchLucidHostingRole.Worker);

        bool hasHosted = services.Any(static d =>
            d.ServiceType == typeof(IHostedService)
            && d.ImplementationType == typeof(TenantHealthScoringHostedService));

        hasHosted.Should().BeTrue();
    }

    [Fact]
    public void AddArchLucidApplicationServices_Api_role_does_not_register_TenantHealthScoringHostedService()
    {
        Dictionary<string, string?> data = CreateWorkerCompositionDictionary();
        data["Hosting:Role"] = "Api";

        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(data).Build();
        ServiceCollection services = CreateCoreServices(configuration);

        _ = services.AddArchLucidApplicationServices(configuration, ArchLucidHostingRole.Api);

        bool hasHosted = services.Any(static d =>
            d.ServiceType == typeof(IHostedService)
            && d.ImplementationType == typeof(TenantHealthScoringHostedService));

        hasHosted.Should().BeFalse();
    }

    private static Dictionary<string, string?> CreateWorkerCompositionDictionary()
    {
        return new Dictionary<string, string?>
        {
            ["Hosting:Role"] = "Worker",
            ["ArchLucid:StorageProvider"] = "InMemory",
            ["ConnectionStrings:ArchLucid"] = "",
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
        };
    }

    private static ServiceCollection CreateCoreServices(IConfiguration configuration)
    {
        ServiceCollection services = [];
        services.AddSingleton(typeof(IConfiguration), configuration);
        services.AddSingleton<IHostEnvironment>(new CompositionTestHostEnvironment(Environments.Development));
        services.AddLogging(static b => b.AddDebug());
        services.AddSingleton<IScopeContextProvider, RegistrationTestScopeContextProvider>();

        return services;
    }

    private sealed class RegistrationTestScopeContextProvider : IScopeContextProvider
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
