using ArchLucid.AgentRuntime;
using ArchLucid.AgentRuntime.Safety;
using ArchLucid.Core.Scoping;
using ArchLucid.Core.Safety;
using ArchLucid.Host.Composition.Startup;
using ArchLucid.Host.Core.Hosting;

using FluentAssertions;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ArchLucid.Host.Composition.Tests;

/// <summary>
/// Builds a real <see cref="ServiceProvider"/> after composition to execute registration lambdas in
/// <c>ArchLucid.Host.Composition</c> (not covered by registration-only tests).
/// </summary>
[Trait("Suite", "Core")]
[Trait("Category", "Unit")]
public sealed class ServiceCollectionExtensionsCompositionResolveTests
{
    [Fact]
    public async Task AddArchLucidApplicationServices_RealAzure_resolves_scoped_IAgentCompletionClient()
    {
        IConfiguration configuration = CreateRealAzureCompositionConfiguration(fallbackLlmEnabled: false);
        ServiceCollection services = CreateCoreServices(configuration);

        services.AddArchLucidApplicationServices(configuration, ArchLucidHostingRole.Api);

        await using ServiceProvider provider = services.BuildServiceProvider();

        await using AsyncServiceScope scope = provider.CreateAsyncScope();
        IAgentCompletionClient client = scope.ServiceProvider.GetRequiredService<IAgentCompletionClient>();

        client.Should().NotBeNull();
        client.Should().BeOfType<CircuitBreakingAgentCompletionClient>();
    }

    [Fact]
    public async Task AddArchLucidApplicationServices_RealAzure_with_FallbackLlm_resolves_FallbackAgentCompletionClient()
    {
        IConfiguration configuration = CreateRealAzureCompositionConfiguration(fallbackLlmEnabled: true);
        ServiceCollection services = CreateCoreServices(configuration);

        services.AddArchLucidApplicationServices(configuration, ArchLucidHostingRole.Api);

        await using ServiceProvider provider = services.BuildServiceProvider();

        await using AsyncServiceScope scope = provider.CreateAsyncScope();
        IAgentCompletionClient client = scope.ServiceProvider.GetRequiredService<IAgentCompletionClient>();

        client.Should().BeOfType<FallbackAgentCompletionClient>();
    }

    [Fact]
    public void AddArchLucidApplicationServices_throws_when_FallbackLlm_enabled_but_incomplete()
    {
        Dictionary<string, string?> data = CreateRealAzureCompositionDictionary(fallbackLlmEnabled: true);
        data["ArchLucid:FallbackLlm:ApiKey"] = "";
        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(data).Build();
        ServiceCollection services = CreateCoreServices(configuration);

        Action act = () => _ = services.AddArchLucidApplicationServices(configuration, ArchLucidHostingRole.Api);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*ArchLucid:FallbackLlm*");
    }

    [Fact]
    public void AddArchLucidApplicationServices_resolves_NullContentSafetyGuard_when_safety_disabled_by_default()
    {
        IConfiguration configuration = CreateRealAzureCompositionConfiguration(fallbackLlmEnabled: false);
        ServiceCollection services = CreateCoreServices(configuration);

        _ = services.AddArchLucidApplicationServices(configuration, ArchLucidHostingRole.Api);

        using ServiceProvider provider = services.BuildServiceProvider();
        IContentSafetyGuard guard = provider.GetRequiredService<IContentSafetyGuard>();

        guard.Should().BeOfType<NullContentSafetyGuard>();
    }

    [Fact]
    public async Task AddArchLucidApplicationServices_resolves_stub_content_safety_guard_when_enabled()
    {
        Dictionary<string, string?> data = CreateRealAzureCompositionDictionary(fallbackLlmEnabled: false);
        data["ArchLucid:ContentSafety:Enabled"] = "true";

        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(data).Build();
        ServiceCollection services = CreateCoreServices(configuration);

        _ = services.AddArchLucidApplicationServices(configuration, ArchLucidHostingRole.Api);

        await using ServiceProvider provider = services.BuildServiceProvider();
        IContentSafetyGuard guard = provider.GetRequiredService<IContentSafetyGuard>();

        guard.Should().BeOfType<ContentSafetyEnabledButUnconfiguredGuard>();

        Func<Task> act = async () =>
            await guard.CheckInputAsync("hello", CancellationToken.None).ConfigureAwait(false);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*ArchLucid:ContentSafety*");
    }

    [Fact]
    public void AddArchLucidApplicationServices_resolves_AzureContentSafetyGuard_when_enabled_with_endpoint_and_key()
    {
        Dictionary<string, string?> data = CreateRealAzureCompositionDictionary(fallbackLlmEnabled: false);
        data["ArchLucid:ContentSafety:Enabled"] = "true";
        data["ArchLucid:ContentSafety:Endpoint"] = "https://unit-test.cognitiveservices.azure.com/";
        data["ArchLucid:ContentSafety:ApiKey"] = "placeholder-key-not-used-in_this_test";

        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(data).Build();
        ServiceCollection services = CreateCoreServices(configuration);

        _ = services.AddArchLucidApplicationServices(configuration, ArchLucidHostingRole.Api);

        using ServiceProvider provider = services.BuildServiceProvider();
        IContentSafetyGuard guard = provider.GetRequiredService<IContentSafetyGuard>();

        guard.Should().BeOfType<AzureContentSafetyGuard>();
    }

    private static ServiceCollection CreateCoreServices(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        ServiceCollection services = new();
        services.AddSingleton(typeof(IConfiguration), configuration);
        services.AddLogging(static b => b.AddDebug());
        services.AddSingleton<IScopeContextProvider, FixedCompositionScopeContextProvider>();

        return services;
    }

    private static IConfiguration CreateRealAzureCompositionConfiguration(bool fallbackLlmEnabled)
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(CreateRealAzureCompositionDictionary(fallbackLlmEnabled))
            .Build();
    }

    private static Dictionary<string, string?> CreateRealAzureCompositionDictionary(bool fallbackLlmEnabled)
    {
        Dictionary<string, string?> data = new()
        {
            ["Hosting:Role"] = "Api",
            ["ConnectionStrings:ArchLucid"] =
                "Server=localhost;Database=ArchLucidCompositionResolveTests;Trusted_Connection=True;TrustServerCertificate=True",
            ["ArchLucid:StorageProvider"] = "InMemory",
            ["AgentExecution:Mode"] = "Real",
            ["AzureOpenAI:Endpoint"] = "https://primary-unit-test.openai.azure.com/",
            ["AzureOpenAI:ApiKey"] = "primary-test-key",
            ["AzureOpenAI:DeploymentName"] = "gpt-primary-test",
            ["AzureOpenAI:EmbeddingDeploymentName"] = "",
            ["FeatureManagement:FeatureFlags:AsyncAuthorityPipeline"] = "false",
            ["RateLimiting:FixedWindow:PermitLimit"] = "100000",
            ["RateLimiting:FixedWindow:WindowMinutes"] = "1",
            ["RateLimiting:Expensive:PermitLimit"] = "100000",
            ["RateLimiting:Expensive:WindowMinutes"] = "1",
            ["LlmCompletionCache:Enabled"] = "false",
        };

        if (fallbackLlmEnabled)
        {
            data["ArchLucid:FallbackLlm:Enabled"] = "true";
            data["ArchLucid:FallbackLlm:Endpoint"] = "https://fallback-unit-test.openai.azure.com/";
            data["ArchLucid:FallbackLlm:ApiKey"] = "fallback-test-key";
            data["ArchLucid:FallbackLlm:DeploymentName"] = "gpt-fallback-test";
        }

        return data;
    }

    private sealed class FixedCompositionScopeContextProvider : IScopeContextProvider
    {
        public ScopeContext GetCurrentScope()
        {
            return new ScopeContext
            {
                TenantId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                WorkspaceId = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                ProjectId = Guid.Parse("33333333-3333-3333-3333-333333333333"),
            };
        }
    }
}
