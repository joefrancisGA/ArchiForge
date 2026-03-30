using ArchiForge.Api.Startup.Diagnostics;

using FluentAssertions;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

using Moq;

namespace ArchiForge.Api.Tests;

public sealed class StartupConfigurationFactsReaderTests
{
    [Fact]
    public void FromConfiguration_maps_expected_flags_and_counts()
    {
        Dictionary<string, string?> data = new()
        {
            ["ConnectionStrings:ArchiForge"] = "Server=.;Database=x;",
            ["ArchiForge:StorageProvider"] = "Sql",
            ["Retrieval:VectorIndex"] = "InMemory",
            ["AgentExecution:Mode"] = "Simulator",
            ["ArchiForgeAuth:Mode"] = "DevelopmentBypass",
            ["Authentication:ApiKey:Enabled"] = "true",
            ["Authentication:ApiKey:AdminKey"] = "secret",
            ["RateLimiting:FixedWindow:PermitLimit"] = "50",
            ["Observability:Prometheus:Enabled"] = "true",
            ["Demo:Enabled"] = "true",
            ["Demo:SeedOnStartup"] = "false",
            ["SchemaValidation:EnableDetailedErrors"] = "true",
            ["Cors:AllowedOrigins:0"] = "https://a.example",
            ["Cors:AllowedOrigins:1"] = "https://b.example",
        };

        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(data)
            .Build();

        Mock<IHostEnvironment> env = new();
        env.SetupGet(e => e.EnvironmentName).Returns("Staging");

        StartupConfigurationFacts facts = StartupConfigurationFactsReader.FromConfiguration(configuration, env.Object);

        facts.HostEnvironmentName.Should().Be("Staging");
        facts.SqlConnectionStringConfigured.Should().BeTrue();
        facts.ArchiForgeStorageProvider.Should().Be("Sql");
        facts.RetrievalVectorIndex.Should().Be("InMemory");
        facts.AgentExecutionMode.Should().Be("Simulator");
        facts.ArchiForgeAuthMode.Should().Be("DevelopmentBypass");
        facts.AuthenticationApiKeyEnabled.Should().BeTrue();
        facts.AuthenticationApiKeyAdminConfigured.Should().BeTrue();
        facts.AuthenticationApiKeyReadOnlyConfigured.Should().BeFalse();
        facts.CorsAllowedOriginCount.Should().Be(2);
        facts.RateLimitingFixedWindowPermitLimit.Should().Be(50);
        facts.ObservabilityPrometheusEnabled.Should().BeTrue();
        facts.DemoEnabled.Should().BeTrue();
        facts.DemoSeedOnStartup.Should().BeFalse();
        facts.SchemaValidationEnableDetailedErrors.Should().BeTrue();
    }

    [Fact]
    public void FromConfiguration_when_keys_missing_uses_placeholders_and_false_flags()
    {
        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>()).Build();
        Mock<IHostEnvironment> env = new();
        env.SetupGet(e => e.EnvironmentName).Returns("Production");

        StartupConfigurationFacts facts = StartupConfigurationFactsReader.FromConfiguration(configuration, env.Object);

        facts.SqlConnectionStringConfigured.Should().BeFalse();
        facts.ArchiForgeStorageProvider.Should().Be("(missing)");
        facts.AuthenticationApiKeyEnabled.Should().BeFalse();
        facts.CorsAllowedOriginCount.Should().Be(0);
    }
}
