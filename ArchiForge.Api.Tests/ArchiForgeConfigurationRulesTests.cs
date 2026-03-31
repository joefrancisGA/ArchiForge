using ArchiForge.Api.Startup.Validation;

using FluentAssertions;

using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

using Moq;

namespace ArchiForge.Api.Tests;

public sealed class ArchiForgeConfigurationRulesTests
{
    [Fact]
    public void CollectErrors_WhenProductionAndDevelopmentBypass_contains_error()
    {
        Dictionary<string, string?> data = new()
        {
            ["ArchiForge:StorageProvider"] = "InMemory",
            ["ArchiForgeAuth:Mode"] = "DevelopmentBypass",
        };

        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(data).Build();
        Mock<IWebHostEnvironment> env = new();
        env.Setup(e => e.EnvironmentName).Returns(Environments.Production);

        IReadOnlyList<string> errors = ArchiForgeConfigurationRules.CollectErrors(configuration, env.Object);

        errors.Should().Contain(e => e.Contains("DevelopmentBypass", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void CollectErrors_WhenProductionAndJwtBearerWithoutAuthority_contains_error()
    {
        Dictionary<string, string?> data = new()
        {
            ["ArchiForge:StorageProvider"] = "InMemory",
            ["ArchiForgeAuth:Mode"] = "JwtBearer",
            ["ArchiForgeAuth:Authority"] = "",
        };

        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(data).Build();
        Mock<IWebHostEnvironment> env = new();
        env.Setup(e => e.EnvironmentName).Returns(Environments.Production);

        IReadOnlyList<string> errors = ArchiForgeConfigurationRules.CollectErrors(configuration, env.Object);

        errors.Should().Contain(e => e.Contains("Authority", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void CollectErrors_WhenProductionAndApiKeyModeButKeysDisabled_contains_error()
    {
        Dictionary<string, string?> data = new()
        {
            ["ArchiForge:StorageProvider"] = "InMemory",
            ["ArchiForgeAuth:Mode"] = "ApiKey",
            ["Authentication:ApiKey:Enabled"] = "false",
            ["Authentication:ApiKey:AdminKey"] = "k",
        };

        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(data).Build();
        Mock<IWebHostEnvironment> env = new();
        env.Setup(e => e.EnvironmentName).Returns(Environments.Production);

        IReadOnlyList<string> errors = ArchiForgeConfigurationRules.CollectErrors(configuration, env.Object);

        errors.Should().Contain(e => e.Contains("Authentication:ApiKey:Enabled", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void CollectErrors_WhenDevelopmentAndDevelopmentBypassAndInMemory_is_empty_when_schema_files_exist()
    {
        Dictionary<string, string?> data = new()
        {
            ["ArchiForge:StorageProvider"] = "InMemory",
            ["ArchiForgeAuth:Mode"] = "DevelopmentBypass",
        };

        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(data).Build();
        Mock<IWebHostEnvironment> env = new();
        env.Setup(e => e.EnvironmentName).Returns(Environments.Development);

        IReadOnlyList<string> errors = ArchiForgeConfigurationRules.CollectErrors(configuration, env.Object);

        errors.Should().BeEmpty();
    }

    [Fact]
    public void CollectErrors_WhenSqlStorageWithoutConnectionString_contains_error()
    {
        Dictionary<string, string?> data = new()
        {
            ["ArchiForge:StorageProvider"] = "Sql",
            ["ArchiForgeAuth:Mode"] = "DevelopmentBypass",
        };

        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(data).Build();
        Mock<IWebHostEnvironment> env = new();
        env.Setup(e => e.EnvironmentName).Returns(Environments.Development);

        IReadOnlyList<string> errors = ArchiForgeConfigurationRules.CollectErrors(configuration, env.Object);

        errors.Should().Contain(e => e.Contains("ConnectionStrings:ArchiForge", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void CollectErrors_WhenInMemoryWithoutConnectionString_is_empty_when_schema_files_exist()
    {
        Dictionary<string, string?> data = new()
        {
            ["ArchiForge:StorageProvider"] = "InMemory",
            ["ArchiForgeAuth:Mode"] = "DevelopmentBypass",
        };

        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(data).Build();
        Mock<IWebHostEnvironment> env = new();
        env.Setup(e => e.EnvironmentName).Returns(Environments.Development);

        IReadOnlyList<string> errors = ArchiForgeConfigurationRules.CollectErrors(configuration, env.Object);

        errors.Should().BeEmpty();
    }

    [Fact]
    public void CollectErrors_WhenStorageProviderIsNotInMemoryOrSql_contains_error()
    {
        Dictionary<string, string?> data = new()
        {
            ["ArchiForge:StorageProvider"] = "Blob",
            ["ArchiForgeAuth:Mode"] = "DevelopmentBypass",
        };

        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(data).Build();
        Mock<IWebHostEnvironment> env = new();
        env.Setup(e => e.EnvironmentName).Returns(Environments.Development);

        IReadOnlyList<string> errors = ArchiForgeConfigurationRules.CollectErrors(configuration, env.Object);

        errors.Should().Contain(e => e.Contains("StorageProvider", StringComparison.OrdinalIgnoreCase)
            && e.Contains("InMemory", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void CollectErrors_WhenAgentExecutionModeIsInvalid_contains_error()
    {
        Dictionary<string, string?> data = new()
        {
            ["ArchiForge:StorageProvider"] = "InMemory",
            ["ArchiForgeAuth:Mode"] = "DevelopmentBypass",
            ["AgentExecution:Mode"] = "Banana",
        };

        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(data).Build();
        Mock<IWebHostEnvironment> env = new();
        env.Setup(e => e.EnvironmentName).Returns(Environments.Development);

        IReadOnlyList<string> errors = ArchiForgeConfigurationRules.CollectErrors(configuration, env.Object);

        errors.Should().Contain(e => e.Contains("AgentExecution:Mode", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void CollectErrors_WhenAgentExecutionModeIsRealWithoutAzureOpenAi_contains_error()
    {
        Dictionary<string, string?> data = new()
        {
            ["ArchiForge:StorageProvider"] = "InMemory",
            ["ArchiForgeAuth:Mode"] = "DevelopmentBypass",
            ["AgentExecution:Mode"] = "Real",
        };

        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(data).Build();
        Mock<IWebHostEnvironment> env = new();
        env.Setup(e => e.EnvironmentName).Returns(Environments.Development);

        IReadOnlyList<string> errors = ArchiForgeConfigurationRules.CollectErrors(configuration, env.Object);

        errors.Should().Contain(e => e.Contains("AzureOpenAI", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void CollectErrors_WhenSchemaPathIsRooted_contains_error()
    {
        Dictionary<string, string?> data = new()
        {
            ["ArchiForge:StorageProvider"] = "InMemory",
            ["ArchiForgeAuth:Mode"] = "DevelopmentBypass",
            ["SchemaValidation:AgentResultSchemaPath"] = "/abs/not-allowed-rooted.schema.json",
        };

        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(data).Build();
        Mock<IWebHostEnvironment> env = new();
        env.Setup(e => e.EnvironmentName).Returns(Environments.Development);

        IReadOnlyList<string> errors = ArchiForgeConfigurationRules.CollectErrors(configuration, env.Object);

        errors.Should().Contain(e => e.Contains("relative", StringComparison.OrdinalIgnoreCase)
            && e.Contains("AgentResult", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void CollectErrors_WhenSchemaPathEscapesBase_contains_error()
    {
        Dictionary<string, string?> data = new()
        {
            ["ArchiForge:StorageProvider"] = "InMemory",
            ["ArchiForgeAuth:Mode"] = "DevelopmentBypass",
            ["SchemaValidation:AgentResultSchemaPath"] = "../outside/agent.schema.json",
        };

        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(data).Build();
        Mock<IWebHostEnvironment> env = new();
        env.Setup(e => e.EnvironmentName).Returns(Environments.Development);

        IReadOnlyList<string> errors = ArchiForgeConfigurationRules.CollectErrors(configuration, env.Object);

        errors.Should().Contain(e => e.Contains("escapes", StringComparison.OrdinalIgnoreCase)
            && e.Contains("AgentResult", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void CollectErrors_WhenBatchMaxBelowMinimum_contains_error()
    {
        Dictionary<string, string?> data = new()
        {
            ["ArchiForge:StorageProvider"] = "InMemory",
            ["ArchiForgeAuth:Mode"] = "DevelopmentBypass",
            ["ComparisonReplay:Batch:MaxComparisonRecordIds"] = "0",
        };

        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(data).Build();
        Mock<IWebHostEnvironment> env = new();
        env.Setup(e => e.EnvironmentName).Returns(Environments.Development);

        IReadOnlyList<string> errors = ArchiForgeConfigurationRules.CollectErrors(configuration, env.Object);

        errors.Should().Contain(e => e.Contains("ComparisonReplay:Batch:MaxComparisonRecordIds", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void CollectErrors_WhenDeprecationEnabledAndSunsetUnparseable_contains_error()
    {
        Dictionary<string, string?> data = new()
        {
            ["ArchiForge:StorageProvider"] = "InMemory",
            ["ArchiForgeAuth:Mode"] = "DevelopmentBypass",
            ["ApiDeprecation:Enabled"] = "true",
            ["ApiDeprecation:SunsetHttpDate"] = "not-a-date",
        };

        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(data).Build();
        Mock<IWebHostEnvironment> env = new();
        env.Setup(e => e.EnvironmentName).Returns(Environments.Development);

        IReadOnlyList<string> errors = ArchiForgeConfigurationRules.CollectErrors(configuration, env.Object);

        errors.Should().Contain(e => e.Contains("ApiDeprecation:SunsetHttpDate", StringComparison.OrdinalIgnoreCase));
    }
}
