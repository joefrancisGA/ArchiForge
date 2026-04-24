using System.Reflection;

using ArchLucid.Host.Core.Startup.Diagnostics;

using FluentAssertions;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

using Moq;

namespace ArchLucid.Api.Tests;

public sealed class StartupConfigurationFactsReaderTests
{
    private static Assembly ApiAssembly => typeof(Program).Assembly;

    [Fact]
    public void FromConfiguration_maps_expected_flags_and_counts()
    {
        Dictionary<string, string?> data = new()
        {
            ["ConnectionStrings:ArchLucid"] = "Server=.;Database=x;",
            ["ArchLucid:StorageProvider"] = "Sql",
            ["Retrieval:VectorIndex"] = "InMemory",
            ["AgentExecution:Mode"] = "Simulator",
            ["ArchLucidAuth:Mode"] = "DevelopmentBypass",
            ["Authentication:ApiKey:Enabled"] = "true",
            ["Authentication:ApiKey:AdminKey"] = "secret",
            ["RateLimiting:FixedWindow:PermitLimit"] = "50",
            ["Observability:Prometheus:Enabled"] = "true",
            ["Observability:Prometheus:RequireScrapeAuthentication"] = "false",
            ["Demo:Enabled"] = "true",
            ["Demo:SeedOnStartup"] = "false",
            ["SchemaValidation:EnableDetailedErrors"] = "true",
            ["Cors:AllowedOrigins:0"] = "https://a.example",
            ["Cors:AllowedOrigins:1"] = "https://b.example"
        };

        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(data)
            .Build();

        Mock<IHostEnvironment> env = new();
        env.SetupGet(e => e.EnvironmentName).Returns("Staging");
        env.SetupGet(e => e.ContentRootPath).Returns("/app/content");

        StartupConfigurationFacts facts = StartupConfigurationFactsReader.FromConfiguration(
            configuration,
            env.Object,
            ApiAssembly);

        facts.BuildInformationalVersion.Should().NotBeNullOrWhiteSpace();
        facts.BuildAssemblyVersion.Should().NotBeNullOrWhiteSpace();
        facts.RuntimeFrameworkDescription.Should().Contain(".NET");

        facts.HostEnvironmentName.Should().Be("Staging");
        facts.ContentRootPath.Should().Be("/app/content");
        facts.SqlConnectionStringConfigured.Should().BeTrue();
        facts.ArchLucidStorageProvider.Should().Be("Sql");
        facts.RetrievalVectorIndex.Should().Be("InMemory");
        facts.AgentExecutionMode.Should().Be("Simulator");
        facts.ArchLucidAuthMode.Should().Be("DevelopmentBypass");
        facts.AuthenticationApiKeyEnabled.Should().BeTrue();
        facts.AuthenticationApiKeyAdminConfigured.Should().BeTrue();
        facts.AuthenticationApiKeyReadOnlyConfigured.Should().BeFalse();
        facts.CorsAllowedOriginCount.Should().Be(2);
        facts.RateLimitingFixedWindowPermitLimit.Should().Be(50);
        facts.ObservabilityPrometheusEnabled.Should().BeTrue();
        facts.DemoEnabled.Should().BeTrue();
        facts.DemoSeedOnStartup.Should().BeFalse();
        facts.SchemaValidationEnableDetailedErrors.Should().BeTrue();
        facts.CosmosDbPolyglotAnyFeatureEnabled.Should().BeFalse();
        facts.CosmosDbConnectivitySummary.Should().Be("disabled");
    }

    [Fact]
    public void FromConfiguration_when_keys_missing_uses_placeholders_and_false_flags()
    {
        IConfiguration configuration =
            new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>()).Build();
        Mock<IHostEnvironment> env = new();
        env.SetupGet(e => e.EnvironmentName).Returns("Production");
        env.SetupGet(e => e.ContentRootPath).Returns(string.Empty);

        StartupConfigurationFacts facts = StartupConfigurationFactsReader.FromConfiguration(
            configuration,
            env.Object,
            ApiAssembly);

        facts.BuildInformationalVersion.Should().NotBeNullOrWhiteSpace();

        facts.ContentRootPath.Should().BeEmpty();
        facts.SqlConnectionStringConfigured.Should().BeFalse();
        facts.ArchLucidStorageProvider.Should().Be("(missing)");
        facts.AuthenticationApiKeyEnabled.Should().BeFalse();
        facts.CorsAllowedOriginCount.Should().Be(0);
        facts.CosmosDbPolyglotAnyFeatureEnabled.Should().BeFalse();
        facts.CosmosDbConnectivitySummary.Should().Be("disabled");
    }
}
