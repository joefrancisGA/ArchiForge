using ArchLucid.Core.Configuration;
using ArchLucid.Host.Core.Startup.Validation;

using FluentAssertions;

using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

using Moq;

namespace ArchLucid.Api.Tests;

public sealed class ArchLucidConfigurationRulesTests
{
    [Fact]
    public void CollectErrors_WhenProductionAndDevelopmentBypass_contains_error()
    {
        Dictionary<string, string?> data = new()
        {
            ["ArchLucid:StorageProvider"] = "InMemory",
            ["ArchLucidAuth:Mode"] = "DevelopmentBypass",
            ["Cors:AllowedOrigins:0"] = "https://ops.example.com",
            ["WebhookDelivery:UseHttpClient"] = "false",
        };

        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(data).Build();
        Mock<IWebHostEnvironment> env = new();
        env.Setup(e => e.EnvironmentName).Returns(Environments.Production);

        IReadOnlyList<string> errors = ArchLucidConfigurationRules.CollectErrors(configuration, env.Object);

        errors.Should().Contain(e => e.Contains("DevelopmentBypass", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void CollectErrors_WhenProductionAndJwtBearerWithoutAuthority_contains_error()
    {
        Dictionary<string, string?> data = new()
        {
            ["ArchLucid:StorageProvider"] = "InMemory",
            ["ArchLucidAuth:Mode"] = "JwtBearer",
            ["ArchLucidAuth:Authority"] = "",
            ["Cors:AllowedOrigins:0"] = "https://ops.example.com",
            ["WebhookDelivery:UseHttpClient"] = "false",
        };

        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(data).Build();
        Mock<IWebHostEnvironment> env = new();
        env.Setup(e => e.EnvironmentName).Returns(Environments.Production);

        IReadOnlyList<string> errors = ArchLucidConfigurationRules.CollectErrors(configuration, env.Object);

        errors.Should().Contain(e => e.Contains("Authority", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void CollectErrors_WhenDevelopmentAndJwtBearerWithPemMissingIssuer_contains_error()
    {
        Dictionary<string, string?> data = new()
        {
            ["ArchLucid:StorageProvider"] = "InMemory",
            ["ArchLucidAuth:Mode"] = "JwtBearer",
            ["ArchLucidAuth:JwtSigningPublicKeyPemPath"] = "/tmp/archlucid-ci-public.pem",
            ["ArchLucidAuth:JwtLocalIssuer"] = "",
            ["ArchLucidAuth:JwtLocalAudience"] = "api://x",
        };

        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(data).Build();
        Mock<IWebHostEnvironment> env = new();
        env.Setup(e => e.EnvironmentName).Returns(Environments.Development);

        IReadOnlyList<string> errors = ArchLucidConfigurationRules.CollectErrors(configuration, env.Object);

        errors.Should().Contain(e => e.Contains("JwtLocalIssuer", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void CollectErrors_WhenProductionAndJwtBearerWithLocalPem_contains_error()
    {
        Dictionary<string, string?> data = new()
        {
            ["ArchLucid:StorageProvider"] = "InMemory",
            ["ArchLucidAuth:Mode"] = "JwtBearer",
            ["ArchLucidAuth:JwtSigningPublicKeyPemPath"] = "/tmp/archlucid-ci-public.pem",
            ["ArchLucidAuth:JwtLocalIssuer"] = "https://ci.local",
            ["ArchLucidAuth:JwtLocalAudience"] = "api://x",
            ["ArchLucidAuth:Authority"] = "",
            ["Cors:AllowedOrigins:0"] = "https://ops.example.com",
            ["WebhookDelivery:UseHttpClient"] = "false",
        };

        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(data).Build();
        Mock<IWebHostEnvironment> env = new();
        env.Setup(e => e.EnvironmentName).Returns(Environments.Production);

        IReadOnlyList<string> errors = ArchLucidConfigurationRules.CollectErrors(configuration, env.Object);

        errors.Should().Contain(e => e.Contains("JwtSigningPublicKeyPemPath", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void CollectErrors_WhenProductionAndApiKeyModeButKeysDisabled_contains_error()
    {
        Dictionary<string, string?> data = new()
        {
            ["ArchLucid:StorageProvider"] = "InMemory",
            ["ArchLucidAuth:Mode"] = "ApiKey",
            ["Authentication:ApiKey:Enabled"] = "false",
            ["Authentication:ApiKey:AdminKey"] = "k",
            ["Cors:AllowedOrigins:0"] = "https://ops.example.com",
            ["WebhookDelivery:UseHttpClient"] = "false",
        };

        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(data).Build();
        Mock<IWebHostEnvironment> env = new();
        env.Setup(e => e.EnvironmentName).Returns(Environments.Production);

        IReadOnlyList<string> errors = ArchLucidConfigurationRules.CollectErrors(configuration, env.Object);

        errors.Should().Contain(e => e.Contains("Authentication:ApiKey:Enabled", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void CollectErrors_WhenProductionAndApiKeyDevelopmentBypassAll_contains_error()
    {
        Dictionary<string, string?> data = new()
        {
            ["ArchLucid:StorageProvider"] = "InMemory",
            ["ArchLucidAuth:Mode"] = "JwtBearer",
            ["ArchLucidAuth:Authority"] = "https://login.microsoftonline.com/tenant/v2.0",
            ["Authentication:ApiKey:DevelopmentBypassAll"] = "true",
            ["Cors:AllowedOrigins:0"] = "https://ops.example.com",
            ["WebhookDelivery:UseHttpClient"] = "false",
        };

        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(data).Build();
        Mock<IWebHostEnvironment> env = new();
        env.Setup(e => e.EnvironmentName).Returns(Environments.Production);

        IReadOnlyList<string> errors = ArchLucidConfigurationRules.CollectErrors(configuration, env.Object);

        errors.Should().Contain(e => e.Contains("Authentication:ApiKey:DevelopmentBypassAll", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void CollectErrors_WhenProductionAndApiKeyEnabledWithPlaceholderAdminKey_contains_error()
    {
        Dictionary<string, string?> data = new()
        {
            ["ArchLucid:StorageProvider"] = "InMemory",
            ["ArchLucidAuth:Mode"] = "JwtBearer",
            ["ArchLucidAuth:Authority"] = "https://login.example.com",
            ["Authentication:ApiKey:Enabled"] = "true",
            ["Authentication:ApiKey:AdminKey"] = "changeme",
            ["Authentication:ApiKey:ReadOnlyKey"] = "",
            ["Cors:AllowedOrigins:0"] = "https://ops.example.com",
            ["WebhookDelivery:UseHttpClient"] = "false",
        };

        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(data).Build();
        Mock<IWebHostEnvironment> env = new();
        env.Setup(e => e.EnvironmentName).Returns(Environments.Production);

        IReadOnlyList<string> errors = ArchLucidConfigurationRules.CollectErrors(configuration, env.Object);

        errors.Should().Contain(e => e.Contains("Authentication:ApiKey:AdminKey", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void CollectErrors_WhenProductionAndApiKeyEnabledWithPlaceholderReadOnlyKey_contains_error()
    {
        Dictionary<string, string?> data = new()
        {
            ["ArchLucid:StorageProvider"] = "InMemory",
            ["ArchLucidAuth:Mode"] = "JwtBearer",
            ["ArchLucidAuth:Authority"] = "https://login.example.com",
            ["Authentication:ApiKey:Enabled"] = "true",
            ["Authentication:ApiKey:AdminKey"] = "abcdefghijklmnopqrstuvwxyz123456",
            ["Authentication:ApiKey:ReadOnlyKey"] = "password",
            ["Cors:AllowedOrigins:0"] = "https://ops.example.com",
            ["WebhookDelivery:UseHttpClient"] = "false",
        };

        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(data).Build();
        Mock<IWebHostEnvironment> env = new();
        env.Setup(e => e.EnvironmentName).Returns(Environments.Production);

        IReadOnlyList<string> errors = ArchLucidConfigurationRules.CollectErrors(configuration, env.Object);

        errors.Should().Contain(e => e.Contains("Authentication:ApiKey:ReadOnlyKey", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void CollectErrors_WhenProductionWorkerAndApiKeyEnabledWithPlaceholderAdminKey_contains_error()
    {
        Dictionary<string, string?> data = new()
        {
            ["Hosting:Role"] = "Worker",
            ["ArchLucid:StorageProvider"] = "InMemory",
            ["ArchLucidAuth:Mode"] = "JwtBearer",
            ["ArchLucidAuth:Authority"] = "https://login.example.com",
            ["Authentication:ApiKey:Enabled"] = "true",
            ["Authentication:ApiKey:AdminKey"] = "your-api-key",
            ["Authentication:ApiKey:ReadOnlyKey"] = "",
            ["WebhookDelivery:UseHttpClient"] = "false",
        };

        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(data).Build();
        Mock<IWebHostEnvironment> env = new();
        env.Setup(e => e.EnvironmentName).Returns(Environments.Production);

        IReadOnlyList<string> errors = ArchLucidConfigurationRules.CollectErrors(configuration, env.Object);

        errors.Should().Contain(e => e.Contains("Authentication:ApiKey:AdminKey", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void CollectErrors_WhenDevelopmentAndApiKeyEnabledWithPlaceholder_does_not_add_production_placeholder_error()
    {
        Dictionary<string, string?> data = new()
        {
            ["ArchLucid:StorageProvider"] = "InMemory",
            ["ArchLucidAuth:Mode"] = "DevelopmentBypass",
            ["Authentication:ApiKey:Enabled"] = "true",
            ["Authentication:ApiKey:AdminKey"] = "changeme",
            ["Authentication:ApiKey:ReadOnlyKey"] = "",
        };

        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(data).Build();
        Mock<IWebHostEnvironment> env = new();
        env.Setup(e => e.EnvironmentName).Returns(Environments.Development);

        IReadOnlyList<string> errors = ArchLucidConfigurationRules.CollectErrors(configuration, env.Object);

        errors.Should().NotContain(e => e.Contains("appears to be a placeholder or weak value", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void CollectErrors_WhenProductionAndApiKeyEnabledWithStrongKeys_has_no_placeholder_error()
    {
        const string strongAdmin = "a7f3c9e2b1d80456n8m0k2j4h6g8f0e2";
        const string strongReader = "b8g4d0f3c2e90567o9n1l3k5i7h9g1f3";

        Dictionary<string, string?> data = new()
        {
            ["ArchLucid:StorageProvider"] = "InMemory",
            ["ArchLucidAuth:Mode"] = "JwtBearer",
            ["ArchLucidAuth:Authority"] = "https://login.example.com",
            ["Authentication:ApiKey:Enabled"] = "true",
            ["Authentication:ApiKey:AdminKey"] = strongAdmin,
            ["Authentication:ApiKey:ReadOnlyKey"] = strongReader,
            ["Cors:AllowedOrigins:0"] = "https://ops.example.com",
            ["WebhookDelivery:UseHttpClient"] = "false",
        };

        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(data).Build();
        Mock<IWebHostEnvironment> env = new();
        env.Setup(e => e.EnvironmentName).Returns(Environments.Production);

        IReadOnlyList<string> errors = ArchLucidConfigurationRules.CollectErrors(configuration, env.Object);

        errors.Should().NotContain(e => e.Contains("appears to be a placeholder or weak value", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void CollectErrors_WhenProductionAndApiKeyEnabledWithTwentyCharAdminKey_has_no_placeholder_error()
    {
        Dictionary<string, string?> data = new()
        {
            ["ArchLucid:StorageProvider"] = "InMemory",
            ["ArchLucidAuth:Mode"] = "JwtBearer",
            ["ArchLucidAuth:Authority"] = "https://login.example.com",
            ["Authentication:ApiKey:Enabled"] = "true",
            ["Authentication:ApiKey:AdminKey"] = "aB3$xK9mN2pQ7wR5vZ1y",
            ["Authentication:ApiKey:ReadOnlyKey"] = "",
            ["Cors:AllowedOrigins:0"] = "https://ops.example.com",
            ["WebhookDelivery:UseHttpClient"] = "false",
        };

        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(data).Build();
        Mock<IWebHostEnvironment> env = new();
        env.Setup(e => e.EnvironmentName).Returns(Environments.Production);

        IReadOnlyList<string> errors = ArchLucidConfigurationRules.CollectErrors(configuration, env.Object);

        errors.Should().NotContain(e => e.Contains("appears to be a placeholder or weak value", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void CollectErrors_WhenProductionAndApiKeyEnabledWithReadOnlyKeyTest_contains_error()
    {
        Dictionary<string, string?> data = new()
        {
            ["ArchLucid:StorageProvider"] = "InMemory",
            ["ArchLucidAuth:Mode"] = "JwtBearer",
            ["ArchLucidAuth:Authority"] = "https://login.example.com",
            ["Authentication:ApiKey:Enabled"] = "true",
            ["Authentication:ApiKey:AdminKey"] = "aB3$xK9mN2pQ7wR5vZ1yabcdefghijklmnopqrst",
            ["Authentication:ApiKey:ReadOnlyKey"] = "test",
            ["Cors:AllowedOrigins:0"] = "https://ops.example.com",
            ["WebhookDelivery:UseHttpClient"] = "false",
        };

        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(data).Build();
        Mock<IWebHostEnvironment> env = new();
        env.Setup(e => e.EnvironmentName).Returns(Environments.Production);

        IReadOnlyList<string> errors = ArchLucidConfigurationRules.CollectErrors(configuration, env.Object);

        errors.Should().Contain(e => e.Contains("Authentication:ApiKey:ReadOnlyKey", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void CollectErrors_WhenProductionAndApiKeyDisabled_weakAdminKey_does_not_add_placeholder_error()
    {
        Dictionary<string, string?> data = new()
        {
            ["ArchLucid:StorageProvider"] = "InMemory",
            ["ArchLucidAuth:Mode"] = "JwtBearer",
            ["ArchLucidAuth:Authority"] = "https://login.example.com",
            ["Authentication:ApiKey:Enabled"] = "false",
            ["Authentication:ApiKey:AdminKey"] = "test",
            ["Cors:AllowedOrigins:0"] = "https://ops.example.com",
            ["WebhookDelivery:UseHttpClient"] = "false",
        };

        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(data).Build();
        Mock<IWebHostEnvironment> env = new();
        env.Setup(e => e.EnvironmentName).Returns(Environments.Production);

        IReadOnlyList<string> errors = ArchLucidConfigurationRules.CollectErrors(configuration, env.Object);

        errors.Should().NotContain(e => e.Contains("appears to be a placeholder or weak value", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void CollectErrors_WhenDevelopmentAndDevelopmentBypassAndInMemory_is_empty_when_schema_files_exist()
    {
        Dictionary<string, string?> data = new()
        {
            ["ArchLucid:StorageProvider"] = "InMemory",
            ["ArchLucidAuth:Mode"] = "DevelopmentBypass",
        };

        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(data).Build();
        Mock<IWebHostEnvironment> env = new();
        env.Setup(e => e.EnvironmentName).Returns(Environments.Development);

        IReadOnlyList<string> errors = ArchLucidConfigurationRules.CollectErrors(configuration, env.Object);

        errors.Should().BeEmpty();
    }

    [Fact]
    public void CollectErrors_WhenSqlStorageWithoutConnectionString_contains_error()
    {
        Dictionary<string, string?> data = new()
        {
            ["ArchLucid:StorageProvider"] = "Sql",
            ["ArchLucidAuth:Mode"] = "DevelopmentBypass",
        };

        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(data).Build();
        Mock<IWebHostEnvironment> env = new();
        env.Setup(e => e.EnvironmentName).Returns(Environments.Development);

        IReadOnlyList<string> errors = ArchLucidConfigurationRules.CollectErrors(configuration, env.Object);

        errors.Should().Contain(e =>
            e.Contains("ConnectionStrings:ArchLucid", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void CollectErrors_WhenInMemoryWithoutConnectionString_is_empty_when_schema_files_exist()
    {
        Dictionary<string, string?> data = new()
        {
            ["ArchLucid:StorageProvider"] = "InMemory",
            ["ArchLucidAuth:Mode"] = "DevelopmentBypass",
        };

        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(data).Build();
        Mock<IWebHostEnvironment> env = new();
        env.Setup(e => e.EnvironmentName).Returns(Environments.Development);

        IReadOnlyList<string> errors = ArchLucidConfigurationRules.CollectErrors(configuration, env.Object);

        errors.Should().BeEmpty();
    }

    [Fact]
    public void CollectErrors_WhenStorageProviderIsNotInMemoryOrSql_contains_error()
    {
        Dictionary<string, string?> data = new()
        {
            ["ArchLucid:StorageProvider"] = "Blob",
            ["ArchLucidAuth:Mode"] = "DevelopmentBypass",
        };

        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(data).Build();
        Mock<IWebHostEnvironment> env = new();
        env.Setup(e => e.EnvironmentName).Returns(Environments.Development);

        IReadOnlyList<string> errors = ArchLucidConfigurationRules.CollectErrors(configuration, env.Object);

        errors.Should().Contain(e => e.Contains("StorageProvider", StringComparison.OrdinalIgnoreCase)
            && e.Contains("InMemory", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void CollectErrors_WhenAgentExecutionModeIsInvalid_contains_error()
    {
        Dictionary<string, string?> data = new()
        {
            ["ArchLucid:StorageProvider"] = "InMemory",
            ["ArchLucidAuth:Mode"] = "DevelopmentBypass",
            ["AgentExecution:Mode"] = "Banana",
        };

        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(data).Build();
        Mock<IWebHostEnvironment> env = new();
        env.Setup(e => e.EnvironmentName).Returns(Environments.Development);

        IReadOnlyList<string> errors = ArchLucidConfigurationRules.CollectErrors(configuration, env.Object);

        errors.Should().Contain(e => e.Contains("AgentExecution:Mode", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void CollectErrors_WhenAgentExecutionModeIsRealWithoutAzureOpenAi_contains_error()
    {
        Dictionary<string, string?> data = new()
        {
            ["ArchLucid:StorageProvider"] = "InMemory",
            ["ArchLucidAuth:Mode"] = "DevelopmentBypass",
            ["AgentExecution:Mode"] = "Real",
        };

        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(data).Build();
        Mock<IWebHostEnvironment> env = new();
        env.Setup(e => e.EnvironmentName).Returns(Environments.Development);

        IReadOnlyList<string> errors = ArchLucidConfigurationRules.CollectErrors(configuration, env.Object);

        errors.Should().Contain(e => e.Contains("AzureOpenAI", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void CollectErrors_WhenRealModeWithEchoCompletionClient_allows_missing_AzureOpenAi()
    {
        Dictionary<string, string?> data = new()
        {
            ["ArchLucid:StorageProvider"] = "InMemory",
            ["ArchLucidAuth:Mode"] = "DevelopmentBypass",
            ["AgentExecution:Mode"] = "Real",
            ["AgentExecution:CompletionClient"] = "Echo",
            ["LlmCompletionCache:Enabled"] = "false",
            ["Cors:AllowedOrigins:0"] = "https://ops.example.com",
            ["WebhookDelivery:UseHttpClient"] = "false",
        };

        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(data).Build();
        Mock<IWebHostEnvironment> env = new();
        env.Setup(e => e.EnvironmentName).Returns(Environments.Development);

        IReadOnlyList<string> errors = ArchLucidConfigurationRules.CollectErrors(configuration, env.Object);

        errors.Should().NotContain(e => e.Contains("AzureOpenAI", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void CollectErrors_WhenTransactionalOutboxEnabledWithInMemory_contains_error()
    {
        Dictionary<string, string?> data = new()
        {
            ["ArchLucid:StorageProvider"] = "InMemory",
            ["ArchLucidAuth:Mode"] = "DevelopmentBypass",
            ["IntegrationEvents:TransactionalOutboxEnabled"] = "true",
            ["LlmCompletionCache:Enabled"] = "false",
            ["Cors:AllowedOrigins:0"] = "https://ops.example.com",
            ["WebhookDelivery:UseHttpClient"] = "false",
        };

        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(data).Build();
        Mock<IWebHostEnvironment> env = new();
        env.Setup(e => e.EnvironmentName).Returns(Environments.Development);

        IReadOnlyList<string> errors = ArchLucidConfigurationRules.CollectErrors(configuration, env.Object);

        errors.Should().Contain(e => e.Contains("TransactionalOutboxEnabled", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void CollectErrors_WhenRealModeAndMaxCompletionTokensNegative_contains_error()
    {
        Dictionary<string, string?> data = new()
        {
            ["ArchLucid:StorageProvider"] = "InMemory",
            ["ArchLucidAuth:Mode"] = "DevelopmentBypass",
            ["AgentExecution:Mode"] = "Real",
            ["AzureOpenAI:Endpoint"] = "https://example.openai.azure.com/",
            ["AzureOpenAI:ApiKey"] = "key",
            ["AzureOpenAI:DeploymentName"] = "dep",
            ["AzureOpenAI:MaxCompletionTokens"] = "-1",
            ["LlmCompletionCache:Enabled"] = "false",
            ["Cors:AllowedOrigins:0"] = "https://ops.example.com",
            ["WebhookDelivery:UseHttpClient"] = "false",
        };

        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(data).Build();
        Mock<IWebHostEnvironment> env = new();
        env.Setup(e => e.EnvironmentName).Returns(Environments.Development);

        IReadOnlyList<string> errors = ArchLucidConfigurationRules.CollectErrors(configuration, env.Object);

        errors.Should().Contain(e => e.Contains("MaxCompletionTokens", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void CollectErrors_WhenLlmCompletionCacheMaxEntriesZero_contains_error()
    {
        Dictionary<string, string?> data = new()
        {
            ["ArchLucid:StorageProvider"] = "InMemory",
            ["ArchLucidAuth:Mode"] = "DevelopmentBypass",
            ["LlmCompletionCache:Enabled"] = "true",
            ["LlmCompletionCache:MaxEntries"] = "0",
            ["Cors:AllowedOrigins:0"] = "https://ops.example.com",
            ["WebhookDelivery:UseHttpClient"] = "false",
        };

        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(data).Build();
        Mock<IWebHostEnvironment> env = new();
        env.Setup(e => e.EnvironmentName).Returns(Environments.Development);

        IReadOnlyList<string> errors = ArchLucidConfigurationRules.CollectErrors(configuration, env.Object);

        errors.Should().Contain(e => e.Contains("LlmCompletionCache:MaxEntries", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void CollectErrors_WhenLlmCompletionCacheDistributedWithoutRedis_contains_error()
    {
        Dictionary<string, string?> data = new()
        {
            ["ArchLucid:StorageProvider"] = "InMemory",
            ["ArchLucidAuth:Mode"] = "DevelopmentBypass",
            ["LlmCompletionCache:Enabled"] = "true",
            ["LlmCompletionCache:Provider"] = "Distributed",
            ["Cors:AllowedOrigins:0"] = "https://ops.example.com",
            ["WebhookDelivery:UseHttpClient"] = "false",
        };

        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(data).Build();
        Mock<IWebHostEnvironment> env = new();
        env.Setup(e => e.EnvironmentName).Returns(Environments.Development);

        IReadOnlyList<string> errors = ArchLucidConfigurationRules.CollectErrors(configuration, env.Object);

        errors.Should()
            .Contain(
                e => e.Contains(
                    "LlmCompletionCache:Provider Distributed requires",
                    StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void CollectErrors_WhenLlmCompletionCacheDistributedAndHotPathRedisConfigured_has_no_distributed_redis_error()
    {
        Dictionary<string, string?> data = new()
        {
            ["ArchLucid:StorageProvider"] = "Sql",
            ["ArchLucidAuth:Mode"] = "DevelopmentBypass",
            ["ConnectionStrings:ArchLucid"] = "Server=.;Database=x;Trusted_Connection=True;TrustServerCertificate=True",
            ["LlmCompletionCache:Enabled"] = "true",
            ["LlmCompletionCache:Provider"] = "Distributed",
            ["HotPathCache:Enabled"] = "true",
            ["HotPathCache:Provider"] = "Redis",
            ["HotPathCache:RedisConnectionString"] = "localhost:6379",
            ["Cors:AllowedOrigins:0"] = "https://ops.example.com",
            ["WebhookDelivery:UseHttpClient"] = "false",
        };

        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(data).Build();
        Mock<IWebHostEnvironment> env = new();
        env.Setup(e => e.EnvironmentName).Returns(Environments.Development);

        IReadOnlyList<string> errors = ArchLucidConfigurationRules.CollectErrors(configuration, env.Object);

        errors.Should()
            .NotContain(
                e => e.Contains(
                    "LlmCompletionCache:Provider Distributed requires",
                    StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void CollectErrors_WhenDurableJobsAndProcessorReceiveBatchSizeInvalid_contains_error()
    {
        Dictionary<string, string?> data = new()
        {
            ["ArchLucid:StorageProvider"] = "Sql",
            ["ArchLucidAuth:Mode"] = "DevelopmentBypass",
            ["ConnectionStrings:ArchLucid"] = "Server=.;Database=x;Trusted_Connection=True;TrustServerCertificate=True",
            ["BackgroundJobs:Mode"] = "Durable",
            ["BackgroundJobs:ResultsContainerName"] = "background-job-results",
            ["BackgroundJobs:ProcessorReceiveBatchSize"] = "99",
            ["ArtifactLargePayload:BlobProvider"] = "AzureBlob",
            ["ArtifactLargePayload:AzureBlobServiceUri"] = "https://st.blob.core.windows.net",
            ["Cors:AllowedOrigins:0"] = "https://ops.example.com",
            ["WebhookDelivery:UseHttpClient"] = "false",
        };

        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(data).Build();
        Mock<IWebHostEnvironment> env = new();
        env.Setup(e => e.EnvironmentName).Returns(Environments.Development);

        IReadOnlyList<string> errors = ArchLucidConfigurationRules.CollectErrors(configuration, env.Object);

        errors.Should().Contain(e => e.Contains("ProcessorReceiveBatchSize", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void CollectErrors_WhenSchemaPathIsRooted_contains_error()
    {
        Dictionary<string, string?> data = new()
        {
            ["ArchLucid:StorageProvider"] = "InMemory",
            ["ArchLucidAuth:Mode"] = "DevelopmentBypass",
            ["SchemaValidation:AgentResultSchemaPath"] = "/abs/not-allowed-rooted.schema.json",
        };

        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(data).Build();
        Mock<IWebHostEnvironment> env = new();
        env.Setup(e => e.EnvironmentName).Returns(Environments.Development);

        IReadOnlyList<string> errors = ArchLucidConfigurationRules.CollectErrors(configuration, env.Object);

        errors.Should().Contain(e => e.Contains("relative", StringComparison.OrdinalIgnoreCase)
            && e.Contains("AgentResult", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void CollectErrors_WhenSchemaPathEscapesBase_contains_error()
    {
        Dictionary<string, string?> data = new()
        {
            ["ArchLucid:StorageProvider"] = "InMemory",
            ["ArchLucidAuth:Mode"] = "DevelopmentBypass",
            ["SchemaValidation:AgentResultSchemaPath"] = "../outside/agent.schema.json",
        };

        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(data).Build();
        Mock<IWebHostEnvironment> env = new();
        env.Setup(e => e.EnvironmentName).Returns(Environments.Development);

        IReadOnlyList<string> errors = ArchLucidConfigurationRules.CollectErrors(configuration, env.Object);

        errors.Should().Contain(e => e.Contains("escapes", StringComparison.OrdinalIgnoreCase)
            && e.Contains("AgentResult", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void CollectErrors_WhenBatchMaxBelowMinimum_contains_error()
    {
        Dictionary<string, string?> data = new()
        {
            ["ArchLucid:StorageProvider"] = "InMemory",
            ["ArchLucidAuth:Mode"] = "DevelopmentBypass",
            ["ComparisonReplay:Batch:MaxComparisonRecordIds"] = "0",
        };

        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(data).Build();
        Mock<IWebHostEnvironment> env = new();
        env.Setup(e => e.EnvironmentName).Returns(Environments.Development);

        IReadOnlyList<string> errors = ArchLucidConfigurationRules.CollectErrors(configuration, env.Object);

        errors.Should().Contain(e => e.Contains("ComparisonReplay:Batch:MaxComparisonRecordIds", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void CollectErrors_WhenDeprecationEnabledAndSunsetUnparseable_contains_error()
    {
        Dictionary<string, string?> data = new()
        {
            ["ArchLucid:StorageProvider"] = "InMemory",
            ["ArchLucidAuth:Mode"] = "DevelopmentBypass",
            ["ApiDeprecation:Enabled"] = "true",
            ["ApiDeprecation:SunsetHttpDate"] = "not-a-date",
        };

        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(data).Build();
        Mock<IWebHostEnvironment> env = new();
        env.Setup(e => e.EnvironmentName).Returns(Environments.Development);

        IReadOnlyList<string> errors = ArchLucidConfigurationRules.CollectErrors(configuration, env.Object);

        errors.Should().Contain(e => e.Contains("ApiDeprecation:SunsetHttpDate", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void CollectErrors_WhenDataArchivalIntervalInvalid_contains_error()
    {
        Dictionary<string, string?> data = new()
        {
            ["ArchLucid:StorageProvider"] = "InMemory",
            ["ArchLucidAuth:Mode"] = "DevelopmentBypass",
            ["DataArchival:IntervalHours"] = "0",
        };

        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(data).Build();
        Mock<IWebHostEnvironment> env = new();
        env.Setup(e => e.EnvironmentName).Returns(Environments.Development);

        IReadOnlyList<string> errors = ArchLucidConfigurationRules.CollectErrors(configuration, env.Object);

        errors.Should().Contain(e => e.Contains("DataArchival:IntervalHours", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void CollectErrors_WhenRateLimitingFixedWindowPermitLimitNegative_contains_error()
    {
        Dictionary<string, string?> data = new()
        {
            ["ArchLucid:StorageProvider"] = "InMemory",
            ["ArchLucidAuth:Mode"] = "DevelopmentBypass",
            ["RateLimiting:FixedWindow:PermitLimit"] = "-1",
        };

        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(data).Build();
        Mock<IWebHostEnvironment> env = new();
        env.Setup(e => e.EnvironmentName).Returns(Environments.Development);

        IReadOnlyList<string> errors = ArchLucidConfigurationRules.CollectErrors(configuration, env.Object);

        errors.Should().Contain(e => e.Contains("RateLimiting:FixedWindow", StringComparison.OrdinalIgnoreCase)
            && e.Contains("PermitLimit", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void CollectErrors_WhenRateLimitingExpensiveWindowMinutesZero_contains_error()
    {
        Dictionary<string, string?> data = new()
        {
            ["ArchLucid:StorageProvider"] = "InMemory",
            ["ArchLucidAuth:Mode"] = "DevelopmentBypass",
            ["RateLimiting:Expensive:WindowMinutes"] = "0",
        };

        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(data).Build();
        Mock<IWebHostEnvironment> env = new();
        env.Setup(e => e.EnvironmentName).Returns(Environments.Development);

        IReadOnlyList<string> errors = ArchLucidConfigurationRules.CollectErrors(configuration, env.Object);

        errors.Should().Contain(e => e.Contains("RateLimiting:Expensive", StringComparison.OrdinalIgnoreCase)
            && e.Contains("WindowMinutes", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void CollectErrors_WhenProductionAndCorsOriginsEmpty_contains_error()
    {
        Dictionary<string, string?> data = new()
        {
            ["ArchLucid:StorageProvider"] = "InMemory",
            ["ArchLucidAuth:Mode"] = "JwtBearer",
            ["ArchLucidAuth:Authority"] = "https://login.example.com",
            ["WebhookDelivery:UseHttpClient"] = "false",
        };

        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(data).Build();
        Mock<IWebHostEnvironment> env = new();
        env.Setup(e => e.EnvironmentName).Returns(Environments.Production);

        IReadOnlyList<string> errors = ArchLucidConfigurationRules.CollectErrors(configuration, env.Object);

        errors.Should().Contain(e => e.Contains("Cors:AllowedOrigins", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void CollectErrors_WhenProductionAndHostingRoleWorker_does_not_require_cors_origins()
    {
        Dictionary<string, string?> data = new()
        {
            ["Hosting:Role"] = "Worker",
            ["ArchLucid:StorageProvider"] = "InMemory",
            ["ArchLucidAuth:Mode"] = "JwtBearer",
            ["ArchLucidAuth:Authority"] = "https://login.example.com",
            ["WebhookDelivery:UseHttpClient"] = "false",
        };

        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(data).Build();
        Mock<IWebHostEnvironment> env = new();
        env.Setup(e => e.EnvironmentName).Returns(Environments.Production);

        IReadOnlyList<string> errors = ArchLucidConfigurationRules.CollectErrors(configuration, env.Object);

        errors.Should().NotContain(e => e.Contains("Cors:AllowedOrigins", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void CollectErrors_WhenProductionAndCorsWildcard_contains_error()
    {
        Dictionary<string, string?> data = new()
        {
            ["ArchLucid:StorageProvider"] = "InMemory",
            ["ArchLucidAuth:Mode"] = "JwtBearer",
            ["ArchLucidAuth:Authority"] = "https://login.example.com",
            ["Cors:AllowedOrigins:0"] = "*",
            ["WebhookDelivery:UseHttpClient"] = "false",
        };

        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(data).Build();
        Mock<IWebHostEnvironment> env = new();
        env.Setup(e => e.EnvironmentName).Returns(Environments.Production);

        IReadOnlyList<string> errors = ArchLucidConfigurationRules.CollectErrors(configuration, env.Object);

        errors.Should().Contain(e => e.Contains("wildcard", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void CollectErrors_WhenProductionAndWebhookHttpWithoutSecret_contains_error()
    {
        Dictionary<string, string?> data = new()
        {
            ["ArchLucid:StorageProvider"] = "InMemory",
            ["ArchLucidAuth:Mode"] = "JwtBearer",
            ["ArchLucidAuth:Authority"] = "https://login.example.com",
            ["Cors:AllowedOrigins:0"] = "https://ops.example.com",
            ["WebhookDelivery:UseHttpClient"] = "true",
            ["WebhookDelivery:HmacSha256SharedSecret"] = "",
        };

        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(data).Build();
        Mock<IWebHostEnvironment> env = new();
        env.Setup(e => e.EnvironmentName).Returns(Environments.Production);

        IReadOnlyList<string> errors = ArchLucidConfigurationRules.CollectErrors(configuration, env.Object);

        errors.Should().Contain(e => e.Contains("WebhookDelivery:HmacSha256SharedSecret", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void CollectErrors_WhenRetrievalVectorIndexInvalid_contains_error()
    {
        Dictionary<string, string?> data = new()
        {
            ["ArchLucid:StorageProvider"] = "InMemory",
            ["ArchLucidAuth:Mode"] = "DevelopmentBypass",
            ["Retrieval:VectorIndex"] = "Elastic",
        };

        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(data).Build();
        Mock<IWebHostEnvironment> env = new();
        env.Setup(e => e.EnvironmentName).Returns(Environments.Development);

        IReadOnlyList<string> errors = ArchLucidConfigurationRules.CollectErrors(configuration, env.Object);

        errors.Should().Contain(e => e.Contains("Retrieval:VectorIndex", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void CollectErrors_WhenProductionAndWebhookSecretTooShort_contains_error()
    {
        Dictionary<string, string?> data = new()
        {
            ["ArchLucid:StorageProvider"] = "InMemory",
            ["ArchLucidAuth:Mode"] = "JwtBearer",
            ["ArchLucidAuth:Authority"] = "https://login.example.com",
            ["Cors:AllowedOrigins:0"] = "https://ops.example.com",
            ["WebhookDelivery:UseHttpClient"] = "true",
            ["WebhookDelivery:HmacSha256SharedSecret"] = "short-secret-not-32-chars",
        };

        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(data).Build();
        Mock<IWebHostEnvironment> env = new();
        env.Setup(e => e.EnvironmentName).Returns(Environments.Production);

        IReadOnlyList<string> errors = ArchLucidConfigurationRules.CollectErrors(configuration, env.Object);

        errors.Should().Contain(e =>
            e.Contains("WebhookDelivery:HmacSha256SharedSecret", StringComparison.OrdinalIgnoreCase) &&
            e.Contains("32", StringComparison.Ordinal));
    }

    [Fact]
    public void CollectErrors_WhenRateLimitingReplayLightQueueLimitNegative_contains_error()
    {
        Dictionary<string, string?> data = new()
        {
            ["ArchLucid:StorageProvider"] = "InMemory",
            ["ArchLucidAuth:Mode"] = "DevelopmentBypass",
            ["RateLimiting:Replay:Light:PermitLimit"] = "10",
            ["RateLimiting:Replay:Light:WindowMinutes"] = "1",
            ["RateLimiting:Replay:Light:QueueLimit"] = "-1",
        };

        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(data).Build();
        Mock<IWebHostEnvironment> env = new();
        env.Setup(e => e.EnvironmentName).Returns(Environments.Development);

        IReadOnlyList<string> errors = ArchLucidConfigurationRules.CollectErrors(configuration, env.Object);

        errors.Should().Contain(e => e.Contains("RateLimiting:Replay:Light", StringComparison.OrdinalIgnoreCase)
            && e.Contains("QueueLimit", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void CollectErrors_WhenApiKeyEnabledButNoKeysConfigured_contains_error()
    {
        Dictionary<string, string?> data = new()
        {
            ["ArchLucid:StorageProvider"] = "InMemory",
            ["ArchLucidAuth:Mode"] = "DevelopmentBypass",
            ["Authentication:ApiKey:Enabled"] = "true",
            ["Authentication:ApiKey:AdminKey"] = "",
            ["Authentication:ApiKey:ReadOnlyKey"] = "",
        };

        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(data).Build();
        Mock<IWebHostEnvironment> env = new();
        env.Setup(e => e.EnvironmentName).Returns(Environments.Development);

        IReadOnlyList<string> errors = ArchLucidConfigurationRules.CollectErrors(configuration, env.Object);

        errors.Should().Contain(e => e.Contains("Authentication:ApiKey:Enabled is true", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void CollectErrors_WhenRetrievalEmbeddingCapsInvalid_contains_errors()
    {
        Dictionary<string, string?> data = new()
        {
            ["ArchLucid:StorageProvider"] = "InMemory",
            ["ArchLucidAuth:Mode"] = "DevelopmentBypass",
            ["Retrieval:EmbeddingCaps:MaxTextsPerEmbeddingRequest"] = "0",
            ["Retrieval:EmbeddingCaps:MaxChunksPerIndexOperation"] = "2000000",
        };

        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(data).Build();
        Mock<IWebHostEnvironment> env = new();
        env.Setup(e => e.EnvironmentName).Returns(Environments.Development);

        IReadOnlyList<string> errors = ArchLucidConfigurationRules.CollectErrors(configuration, env.Object);

        errors.Should().Contain(e => e.Contains("Retrieval:EmbeddingCaps:MaxTextsPerEmbeddingRequest", StringComparison.OrdinalIgnoreCase));
        errors.Should().Contain(e => e.Contains("Retrieval:EmbeddingCaps:MaxChunksPerIndexOperation", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void CollectErrors_WhenDataArchivalRunsRetentionOutOfRange_contains_error()
    {
        Dictionary<string, string?> data = new()
        {
            ["ArchLucid:StorageProvider"] = "InMemory",
            ["ArchLucidAuth:Mode"] = "DevelopmentBypass",
            ["DataArchival:RunsRetentionDays"] = "-5",
        };

        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(data).Build();
        Mock<IWebHostEnvironment> env = new();
        env.Setup(e => e.EnvironmentName).Returns(Environments.Development);

        IReadOnlyList<string> errors = ArchLucidConfigurationRules.CollectErrors(configuration, env.Object);

        errors.Should().Contain(e => e.Contains("DataArchival:RunsRetentionDays", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void CollectErrors_WhenBatchReplayMaxIdsAbove500_contains_error()
    {
        Dictionary<string, string?> data = new()
        {
            ["ArchLucid:StorageProvider"] = "InMemory",
            ["ArchLucidAuth:Mode"] = "DevelopmentBypass",
            ["ComparisonReplay:Batch:MaxComparisonRecordIds"] = "501",
        };

        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(data).Build();
        Mock<IWebHostEnvironment> env = new();
        env.Setup(e => e.EnvironmentName).Returns(Environments.Development);

        IReadOnlyList<string> errors = ArchLucidConfigurationRules.CollectErrors(configuration, env.Object);

        errors.Should().Contain(e => e.Contains("ComparisonReplay:Batch:MaxComparisonRecordIds", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void CollectErrors_WhenSchemaPathEmpty_contains_error()
    {
        Dictionary<string, string?> data = new()
        {
            ["ArchLucid:StorageProvider"] = "InMemory",
            ["ArchLucidAuth:Mode"] = "DevelopmentBypass",
            ["SchemaValidation:AgentResultSchemaPath"] = "   ",
        };

        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(data).Build();
        Mock<IWebHostEnvironment> env = new();
        env.Setup(e => e.EnvironmentName).Returns(Environments.Development);

        IReadOnlyList<string> errors = ArchLucidConfigurationRules.CollectErrors(configuration, env.Object);

        errors.Should().Contain(e => e.Contains("AgentResult", StringComparison.OrdinalIgnoreCase)
            && e.Contains("missing", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void CollectErrors_WhenProductionApiKeyModeButBothKeysMissing_contains_error()
    {
        Dictionary<string, string?> data = new()
        {
            ["ArchLucid:StorageProvider"] = "InMemory",
            ["ArchLucidAuth:Mode"] = "ApiKey",
            ["Authentication:ApiKey:Enabled"] = "true",
            ["Authentication:ApiKey:AdminKey"] = "",
            ["Authentication:ApiKey:ReadOnlyKey"] = "",
            ["Cors:AllowedOrigins:0"] = "https://ops.example.com",
            ["WebhookDelivery:UseHttpClient"] = "false",
        };

        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(data).Build();
        Mock<IWebHostEnvironment> env = new();
        env.Setup(e => e.EnvironmentName).Returns(Environments.Production);

        IReadOnlyList<string> errors = ArchLucidConfigurationRules.CollectErrors(configuration, env.Object);

        errors.Should().Contain(e => e.Contains("Production ApiKey auth requires", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void CollectErrors_WhenHotPathCacheEnabledWithInvalidProvider_contains_error()
    {
        Dictionary<string, string?> data = new()
        {
            ["ArchLucid:StorageProvider"] = "InMemory",
            ["ArchLucidAuth:Mode"] = "DevelopmentBypass",
            ["HotPathCache:Enabled"] = "true",
            ["HotPathCache:Provider"] = "CosmosDb",
        };

        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(data).Build();
        Mock<IWebHostEnvironment> env = new();
        env.Setup(e => e.EnvironmentName).Returns(Environments.Development);

        IReadOnlyList<string> errors = ArchLucidConfigurationRules.CollectErrors(configuration, env.Object);

        errors.Should().Contain(e => e.Contains("HotPathCache:Provider", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void CollectErrors_WhenHotPathCacheRedisWithoutConnectionString_contains_error()
    {
        Dictionary<string, string?> data = new()
        {
            ["ArchLucid:StorageProvider"] = "InMemory",
            ["ArchLucidAuth:Mode"] = "DevelopmentBypass",
            ["HotPathCache:Enabled"] = "true",
            ["HotPathCache:Provider"] = "Redis",
            ["HotPathCache:RedisConnectionString"] = "",
        };

        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(data).Build();
        Mock<IWebHostEnvironment> env = new();
        env.Setup(e => e.EnvironmentName).Returns(Environments.Development);

        IReadOnlyList<string> errors = ArchLucidConfigurationRules.CollectErrors(configuration, env.Object);

        errors.Should().Contain(e => e.Contains("HotPathCache:RedisConnectionString", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void CollectErrors_WhenHotPathCacheTtlAbove3600_contains_error()
    {
        Dictionary<string, string?> data = new()
        {
            ["ArchLucid:StorageProvider"] = "InMemory",
            ["ArchLucidAuth:Mode"] = "DevelopmentBypass",
            ["HotPathCache:Enabled"] = "true",
            ["HotPathCache:AbsoluteExpirationSeconds"] = "4000",
        };

        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(data).Build();
        Mock<IWebHostEnvironment> env = new();
        env.Setup(e => e.EnvironmentName).Returns(Environments.Development);

        IReadOnlyList<string> errors = ArchLucidConfigurationRules.CollectErrors(configuration, env.Object);

        errors.Should().Contain(e => e.Contains("HotPathCache:AbsoluteExpirationSeconds", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void CollectErrors_WhenHotPathCacheAutoMultiReplicaWithoutRedisInProduction_contains_error()
    {
        Dictionary<string, string?> data = new()
        {
            ["ArchLucid:StorageProvider"] = "InMemory",
            ["ArchLucidAuth:Mode"] = "JwtBearer",
            ["ArchLucidAuth:Authority"] = "https://login.example.com",
            ["Cors:AllowedOrigins:0"] = "https://ops.example.com",
            ["WebhookDelivery:UseHttpClient"] = "false",
            ["HotPathCache:Enabled"] = "true",
            ["HotPathCache:Provider"] = "Auto",
            ["HotPathCache:ExpectedApiReplicaCount"] = "2",
            ["HotPathCache:RedisConnectionString"] = "",
        };

        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(data).Build();
        Mock<IWebHostEnvironment> env = new();
        env.Setup(e => e.EnvironmentName).Returns(Environments.Production);

        IReadOnlyList<string> errors = ArchLucidConfigurationRules.CollectErrors(configuration, env.Object);

        errors.Should().Contain(e => e.Contains("HotPathCache:RedisConnectionString", StringComparison.OrdinalIgnoreCase)
            && e.Contains("ExpectedApiReplicaCount", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void CollectErrors_WhenHotPathCacheAutoMultiReplicaWithoutRedisInDevelopment_does_not_add_replica_redis_error()
    {
        Dictionary<string, string?> data = new()
        {
            ["ArchLucid:StorageProvider"] = "InMemory",
            ["ArchLucidAuth:Mode"] = "DevelopmentBypass",
            ["HotPathCache:Enabled"] = "true",
            ["HotPathCache:Provider"] = "Auto",
            ["HotPathCache:ExpectedApiReplicaCount"] = "5",
            ["HotPathCache:RedisConnectionString"] = "",
        };

        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(data).Build();
        Mock<IWebHostEnvironment> env = new();
        env.Setup(e => e.EnvironmentName).Returns(Environments.Development);

        IReadOnlyList<string> errors = ArchLucidConfigurationRules.CollectErrors(configuration, env.Object);

        errors.Should().NotContain(e => e.Contains("greater than 1 outside Development", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void CollectErrors_WhenHostLeaderElectionRenewNotLessThanLease_contains_error()
    {
        Dictionary<string, string?> data = new()
        {
            ["ArchLucid:StorageProvider"] = "InMemory",
            ["ArchLucidAuth:Mode"] = "DevelopmentBypass",
            ["HostLeaderElection:Enabled"] = "true",
            ["HostLeaderElection:LeaseDurationSeconds"] = "30",
            ["HostLeaderElection:RenewIntervalSeconds"] = "30",
        };

        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(data).Build();
        Mock<IWebHostEnvironment> env = new();
        env.Setup(e => e.EnvironmentName).Returns(Environments.Development);

        IReadOnlyList<string> errors = ArchLucidConfigurationRules.CollectErrors(configuration, env.Object);

        errors.Should().Contain(e => e.Contains("HostLeaderElection:RenewIntervalSeconds", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void CollectErrors_WhenHostLeaderElectionDisabled_allows_renew_equal_to_lease_in_config()
    {
        Dictionary<string, string?> data = new()
        {
            ["ArchLucid:StorageProvider"] = "InMemory",
            ["ArchLucidAuth:Mode"] = "DevelopmentBypass",
            ["HostLeaderElection:Enabled"] = "false",
            ["HostLeaderElection:LeaseDurationSeconds"] = "30",
            ["HostLeaderElection:RenewIntervalSeconds"] = "30",
        };

        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(data).Build();
        Mock<IWebHostEnvironment> env = new();
        env.Setup(e => e.EnvironmentName).Returns(Environments.Development);

        IReadOnlyList<string> errors = ArchLucidConfigurationRules.CollectErrors(configuration, env.Object);

        errors.Should().NotContain(e => e.Contains("HostLeaderElection", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void CollectErrors_WhenOtlpEnabledWithoutEndpoint_contains_error()
    {
        Dictionary<string, string?> data = new()
        {
            ["ArchLucid:StorageProvider"] = "InMemory",
            ["ArchLucidAuth:Mode"] = "DevelopmentBypass",
            ["Observability:Otlp:Enabled"] = "true",
        };

        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(data).Build();
        Mock<IWebHostEnvironment> env = new();
        env.Setup(e => e.EnvironmentName).Returns(Environments.Development);

        IReadOnlyList<string> errors = ArchLucidConfigurationRules.CollectErrors(configuration, env.Object);

        errors.Should().Contain(e => e.Contains("Observability:Otlp:Endpoint", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void CollectErrors_WhenOtlpProtocolInvalid_contains_error()
    {
        Dictionary<string, string?> data = new()
        {
            ["ArchLucid:StorageProvider"] = "InMemory",
            ["ArchLucidAuth:Mode"] = "DevelopmentBypass",
            ["Observability:Otlp:Enabled"] = "true",
            ["Observability:Otlp:Endpoint"] = "http://localhost:4317",
            ["Observability:Otlp:Protocol"] = "Udp",
        };

        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(data).Build();
        Mock<IWebHostEnvironment> env = new();
        env.Setup(e => e.EnvironmentName).Returns(Environments.Development);

        IReadOnlyList<string> errors = ArchLucidConfigurationRules.CollectErrors(configuration, env.Object);

        errors.Should().Contain(e => e.Contains("Observability:Otlp:Protocol", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void CollectErrors_WhenPrometheusEnabledWithoutScrapeCredentials_contains_error()
    {
        Dictionary<string, string?> data = new()
        {
            ["ArchLucid:StorageProvider"] = "InMemory",
            ["ArchLucidAuth:Mode"] = "DevelopmentBypass",
            ["Observability:Prometheus:Enabled"] = "true",
        };

        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(data).Build();
        Mock<IWebHostEnvironment> env = new();
        env.Setup(e => e.EnvironmentName).Returns(Environments.Development);

        IReadOnlyList<string> errors = ArchLucidConfigurationRules.CollectErrors(configuration, env.Object);

        errors.Should().Contain(e => e.Contains("ScrapeUsername", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void CollectErrors_WhenPrometheusEnabledWithScrapeCredentials_has_no_observability_error()
    {
        Dictionary<string, string?> data = new()
        {
            ["ArchLucid:StorageProvider"] = "InMemory",
            ["ArchLucidAuth:Mode"] = "DevelopmentBypass",
            ["Observability:Prometheus:Enabled"] = "true",
            ["Observability:Prometheus:ScrapeUsername"] = "prom",
            ["Observability:Prometheus:ScrapePassword"] = "secret",
        };

        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(data).Build();
        Mock<IWebHostEnvironment> env = new();
        env.Setup(e => e.EnvironmentName).Returns(Environments.Development);

        IReadOnlyList<string> errors = ArchLucidConfigurationRules.CollectErrors(configuration, env.Object);

        errors.Should().NotContain(e => e.Contains("Observability:Prometheus", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void CollectErrors_WhenPrometheusRequireAuthDisabled_allows_missing_scrape_credentials()
    {
        Dictionary<string, string?> data = new()
        {
            ["ArchLucid:StorageProvider"] = "InMemory",
            ["ArchLucidAuth:Mode"] = "DevelopmentBypass",
            ["Observability:Prometheus:Enabled"] = "true",
            ["Observability:Prometheus:RequireScrapeAuthentication"] = "false",
        };

        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(data).Build();
        Mock<IWebHostEnvironment> env = new();
        env.Setup(e => e.EnvironmentName).Returns(Environments.Development);

        IReadOnlyList<string> errors = ArchLucidConfigurationRules.CollectErrors(configuration, env.Object);

        errors.Should().NotContain(e => e.Contains("ScrapeUsername", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void CollectErrors_WhenDevelopmentAndSqlWithoutRlsSessionContext_has_no_row_level_security_error()
    {
        Dictionary<string, string?> data = new()
        {
            ["ArchLucid:StorageProvider"] = "Sql",
            ["ArchLucidAuth:Mode"] = "DevelopmentBypass",
            ["ConnectionStrings:ArchLucid"] = "Server=.;Database=x;Trusted_Connection=True;TrustServerCertificate=True",
            ["SqlServer:RowLevelSecurity:ApplySessionContext"] = "false",
            ["WebhookDelivery:UseHttpClient"] = "false",
        };

        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(data).Build();
        Mock<IWebHostEnvironment> env = new();
        env.Setup(e => e.EnvironmentName).Returns(Environments.Development);

        IReadOnlyList<string> errors = ArchLucidConfigurationRules.CollectErrors(configuration, env.Object);

        errors.Should()
            .NotContain(e => e.Contains("ApplySessionContext", StringComparison.OrdinalIgnoreCase)
                && e.Contains("RowLevelSecurity", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void CollectErrors_WhenProductionApiAndSqlWithoutRlsSessionContext_contains_error()
    {
        Dictionary<string, string?> data = new()
        {
            ["ArchLucid:StorageProvider"] = "Sql",
            ["ArchLucidAuth:Mode"] = "JwtBearer",
            ["ArchLucidAuth:Authority"] = "https://login.example.com",
            ["ConnectionStrings:ArchLucid"] = "Server=.;Database=x;Trusted_Connection=True;TrustServerCertificate=True",
            ["SqlServer:RowLevelSecurity:ApplySessionContext"] = "false",
            ["Cors:AllowedOrigins:0"] = "https://ops.example.com",
            ["WebhookDelivery:UseHttpClient"] = "false",
        };

        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(data).Build();
        Mock<IWebHostEnvironment> env = new();
        env.Setup(e => e.EnvironmentName).Returns(Environments.Production);

        IReadOnlyList<string> errors = ArchLucidConfigurationRules.CollectErrors(configuration, env.Object);

        errors.Should().Contain(e => e.Contains("SqlServer:RowLevelSecurity:ApplySessionContext=true", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void CollectErrors_WhenProductionApiAndSqlWithRlsSessionContext_has_no_row_level_security_error()
    {
        Dictionary<string, string?> data = new()
        {
            ["ArchLucid:StorageProvider"] = "Sql",
            ["ArchLucidAuth:Mode"] = "JwtBearer",
            ["ArchLucidAuth:Authority"] = "https://login.example.com",
            ["ConnectionStrings:ArchLucid"] = "Server=.;Database=x;Trusted_Connection=True;TrustServerCertificate=True",
            ["SqlServer:RowLevelSecurity:ApplySessionContext"] = "true",
            ["Cors:AllowedOrigins:0"] = "https://ops.example.com",
            ["WebhookDelivery:UseHttpClient"] = "false",
        };

        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(data).Build();
        Mock<IWebHostEnvironment> env = new();
        env.Setup(e => e.EnvironmentName).Returns(Environments.Production);

        IReadOnlyList<string> errors = ArchLucidConfigurationRules.CollectErrors(configuration, env.Object);

        errors.Should()
            .NotContain(e => e.Contains("SqlServer:RowLevelSecurity:ApplySessionContext=true", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void CollectErrors_WhenProductionWorkerAndSqlWithoutRlsSessionContext_contains_error()
    {
        Dictionary<string, string?> data = new()
        {
            ["Hosting:Role"] = "Worker",
            ["ArchLucid:StorageProvider"] = "Sql",
            ["ArchLucidAuth:Mode"] = "JwtBearer",
            ["ArchLucidAuth:Authority"] = "https://login.example.com",
            ["ConnectionStrings:ArchLucid"] = "Server=.;Database=x;Trusted_Connection=True;TrustServerCertificate=True",
            ["SqlServer:RowLevelSecurity:ApplySessionContext"] = "false",
            ["WebhookDelivery:UseHttpClient"] = "false",
        };

        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(data).Build();
        Mock<IWebHostEnvironment> env = new();
        env.Setup(e => e.EnvironmentName).Returns(Environments.Production);

        IReadOnlyList<string> errors = ArchLucidConfigurationRules.CollectErrors(configuration, env.Object);

        errors.Should().Contain(e => e.Contains("SqlServer:RowLevelSecurity:ApplySessionContext=true", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void CollectErrors_WhenStagingAndSqlWithoutRlsSessionContext_contains_error()
    {
        Dictionary<string, string?> data = new()
        {
            ["ArchLucid:StorageProvider"] = "Sql",
            ["ArchLucidAuth:Mode"] = "DevelopmentBypass",
            ["ConnectionStrings:ArchLucid"] = "Server=.;Database=x;Trusted_Connection=True;TrustServerCertificate=True",
            ["SqlServer:RowLevelSecurity:ApplySessionContext"] = "false",
            ["WebhookDelivery:UseHttpClient"] = "false",
        };

        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(data).Build();
        Mock<IWebHostEnvironment> env = new();
        env.Setup(e => e.EnvironmentName).Returns(Environments.Staging);

        IReadOnlyList<string> errors = ArchLucidConfigurationRules.CollectErrors(configuration, env.Object);

        errors.Should().Contain(e => e.Contains("SqlServer:RowLevelSecurity:ApplySessionContext=true", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void CollectErrors_WhenLlmTokenQuotaEnabledWithoutPositiveMax_contains_error()
    {
        Dictionary<string, string?> data = new()
        {
            ["ArchLucid:StorageProvider"] = "InMemory",
            ["ArchLucidAuth:Mode"] = "DevelopmentBypass",
            ["LlmTokenQuota:Enabled"] = "true",
            ["LlmTokenQuota:MaxPromptTokensPerTenantPerWindow"] = "0",
            ["LlmTokenQuota:MaxCompletionTokensPerTenantPerWindow"] = "0",
            ["WebhookDelivery:UseHttpClient"] = "false",
        };

        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(data).Build();
        Mock<IWebHostEnvironment> env = new();
        env.Setup(e => e.EnvironmentName).Returns(Environments.Development);

        IReadOnlyList<string> errors = ArchLucidConfigurationRules.CollectErrors(configuration, env.Object);

        errors.Should().Contain(e => e.Contains("LlmTokenQuota:MaxPromptTokensPerTenantPerWindow", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void CollectErrors_WhenLlmTokenQuotaEnabledWithInvalidWindow_contains_error()
    {
        Dictionary<string, string?> data = new()
        {
            ["ArchLucid:StorageProvider"] = "InMemory",
            ["ArchLucidAuth:Mode"] = "DevelopmentBypass",
            ["LlmTokenQuota:Enabled"] = "true",
            ["LlmTokenQuota:WindowMinutes"] = "0",
            ["LlmTokenQuota:MaxPromptTokensPerTenantPerWindow"] = "100",
            ["WebhookDelivery:UseHttpClient"] = "false",
        };

        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(data).Build();
        Mock<IWebHostEnvironment> env = new();
        env.Setup(e => e.EnvironmentName).Returns(Environments.Development);

        IReadOnlyList<string> errors = ArchLucidConfigurationRules.CollectErrors(configuration, env.Object);

        errors.Should().Contain(e => e.Contains("LlmTokenQuota:WindowMinutes", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void CollectErrors_WhenLlmTokenQuotaDisabled_skips_quota_validation()
    {
        Dictionary<string, string?> data = new()
        {
            ["ArchLucid:StorageProvider"] = "InMemory",
            ["ArchLucidAuth:Mode"] = "DevelopmentBypass",
            ["LlmTokenQuota:Enabled"] = "false",
            ["LlmTokenQuota:WindowMinutes"] = "0",
            ["WebhookDelivery:UseHttpClient"] = "false",
        };

        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(data).Build();
        Mock<IWebHostEnvironment> env = new();
        env.Setup(e => e.EnvironmentName).Returns(Environments.Development);

        IReadOnlyList<string> errors = ArchLucidConfigurationRules.CollectErrors(configuration, env.Object);

        errors.Should().NotContain(e => e.Contains("LlmTokenQuota", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void CollectErrors_WhenCosmosFeatureEnabledWithoutConnectionString_contains_error()
    {
        Dictionary<string, string?> data = new()
        {
            ["ArchLucid:StorageProvider"] = "InMemory",
            ["ArchLucidAuth:Mode"] = "DevelopmentBypass",
            ["CosmosDb:GraphSnapshotsEnabled"] = "true",
            ["CosmosDb:ConnectionString"] = "",
            ["WebhookDelivery:UseHttpClient"] = "false",
        };

        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(data).Build();
        Mock<IWebHostEnvironment> env = new();
        env.Setup(e => e.EnvironmentName).Returns(Environments.Development);

        IReadOnlyList<string> errors = ArchLucidConfigurationRules.CollectErrors(configuration, env.Object);

        errors.Should().Contain(e => e.Contains("CosmosDb:ConnectionString", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void CollectErrors_WhenProductionApiAndCosmosEmulatorEndpoint_contains_error()
    {
        Dictionary<string, string?> data = new()
        {
            ["ArchLucid:StorageProvider"] = "Sql",
            ["ArchLucidAuth:Mode"] = "JwtBearer",
            ["ArchLucidAuth:Authority"] = "https://login.example.com",
            ["ConnectionStrings:ArchLucid"] = "Server=.;Database=x;Trusted_Connection=True;TrustServerCertificate=True",
            ["SqlServer:RowLevelSecurity:ApplySessionContext"] = "true",
            ["Cors:AllowedOrigins:0"] = "https://ops.example.com",
            ["WebhookDelivery:UseHttpClient"] = "false",
            ["CosmosDb:AgentTracesEnabled"] = "true",
            ["CosmosDb:ConnectionString"] = "AccountEndpoint=https://localhost:8081/;AccountKey=dummy",
        };

        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(data).Build();
        Mock<IWebHostEnvironment> env = new();
        env.Setup(e => e.EnvironmentName).Returns(Environments.Production);

        IReadOnlyList<string> errors = ArchLucidConfigurationRules.CollectErrors(configuration, env.Object);

        errors.Should().Contain(e => e.Contains("Cosmos Emulator", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void CollectErrors_WhenProductionAndRequireJwtBearerInProductionWithApiKey_contains_error()
    {
        Dictionary<string, string?> data = new()
        {
            ["ArchLucid:StorageProvider"] = "InMemory",
            ["ArchLucidAuth:Mode"] = "ApiKey",
            ["ArchLucidAuth:RequireJwtBearerInProduction"] = "true",
            ["Authentication:ApiKey:Enabled"] = "true",
            ["Authentication:ApiKey:AdminKey"] = "abcdefghijklmnopqrstuvwxyz1234567890abcd",
            ["Cors:AllowedOrigins:0"] = "https://ops.example.com",
            ["WebhookDelivery:UseHttpClient"] = "false",
        };

        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(data).Build();
        Mock<IWebHostEnvironment> env = new();
        env.Setup(e => e.EnvironmentName).Returns(Environments.Production);

        IReadOnlyList<string> errors = ArchLucidConfigurationRules.CollectErrors(configuration, env.Object);

        errors.Should().Contain(e => e.Contains("RequireJwtBearerInProduction", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void CollectErrors_WhenProductionAndRequireJwtBearerInProductionWithJwtBearer_allows_when_authority_configured()
    {
        Dictionary<string, string?> data = new()
        {
            ["ArchLucid:StorageProvider"] = "InMemory",
            ["ArchLucidAuth:Mode"] = "JwtBearer",
            ["ArchLucidAuth:RequireJwtBearerInProduction"] = "true",
            ["ArchLucidAuth:Authority"] = "https://login.microsoftonline.com/tenant/v2.0",
            ["Cors:AllowedOrigins:0"] = "https://ops.example.com",
            ["WebhookDelivery:UseHttpClient"] = "false",
        };

        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(data).Build();
        Mock<IWebHostEnvironment> env = new();
        env.Setup(e => e.EnvironmentName).Returns(Environments.Production);

        IReadOnlyList<string> errors = ArchLucidConfigurationRules.CollectErrors(configuration, env.Object);

        errors.Should().NotContain(e => e.Contains("RequireJwtBearerInProduction", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void CollectErrors_WhenProductionAndMsaExternalIdWithoutExternalIdTenantId_contains_error()
    {
        Dictionary<string, string?> data = new()
        {
            ["ArchLucid:StorageProvider"] = "InMemory",
            ["ArchLucidAuth:Mode"] = "JwtBearer",
            ["ArchLucidAuth:Authority"] = "https://login.microsoftonline.com/common/v2.0",
            ["Cors:AllowedOrigins:0"] = "https://ops.example.com",
            ["WebhookDelivery:UseHttpClient"] = "false",
            ["Auth:Trial:Modes:0"] = "MsaExternalId",
            ["Auth:Trial:ExternalIdTenantId"] = "",
        };

        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(data).Build();
        Mock<IWebHostEnvironment> env = new();
        env.Setup(e => e.EnvironmentName).Returns(Environments.Production);

        IReadOnlyList<string> errors = ArchLucidConfigurationRules.CollectErrors(configuration, env.Object);

        errors.Should().Contain(e => e.Contains("ExternalIdTenantId", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void CollectErrors_WhenProductionAndMsaExternalIdWithExternalIdTenantId_allows()
    {
        Dictionary<string, string?> data = new()
        {
            ["ArchLucid:StorageProvider"] = "InMemory",
            ["ArchLucidAuth:Mode"] = "JwtBearer",
            ["ArchLucidAuth:Authority"] = "https://login.microsoftonline.com/common/v2.0",
            ["Cors:AllowedOrigins:0"] = "https://ops.example.com",
            ["WebhookDelivery:UseHttpClient"] = "false",
            ["Auth:Trial:Modes:0"] = "MsaExternalId",
            ["Auth:Trial:ExternalIdTenantId"] = "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
        };

        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(data).Build();
        Mock<IWebHostEnvironment> env = new();
        env.Setup(e => e.EnvironmentName).Returns(Environments.Production);

        IReadOnlyList<string> errors = ArchLucidConfigurationRules.CollectErrors(configuration, env.Object);

        errors.Should().NotContain(e => e.Contains("ExternalIdTenantId", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void CollectErrors_WhenProductionApiAndAcsEmailWithoutEndpoint_contains_error()
    {
        Dictionary<string, string?> data = new()
        {
            ["ArchLucid:StorageProvider"] = "InMemory",
            ["ArchLucidAuth:Mode"] = "JwtBearer",
            ["ArchLucidAuth:Authority"] = "https://login.example.com",
            ["Cors:AllowedOrigins:0"] = "https://ops.example.com",
            ["WebhookDelivery:UseHttpClient"] = "false",
            ["Email:Provider"] = EmailProviderNames.AzureCommunicationServices,
        };

        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(data).Build();
        Mock<IWebHostEnvironment> env = new();
        env.Setup(e => e.EnvironmentName).Returns(Environments.Production);

        IReadOnlyList<string> errors = ArchLucidConfigurationRules.CollectErrors(configuration, env.Object);

        errors.Should()
            .Contain(e => e.Contains("Email:AzureCommunicationServicesEndpoint", StringComparison.Ordinal));
    }

    [Fact]
    public void CollectErrors_WhenProductionWorkerAndAcsEmailWithoutEndpoint_contains_error()
    {
        Dictionary<string, string?> data = new()
        {
            ["Hosting:Role"] = "Worker",
            ["ArchLucid:StorageProvider"] = "InMemory",
            ["ArchLucidAuth:Mode"] = "JwtBearer",
            ["ArchLucidAuth:Authority"] = "https://login.example.com",
            ["WebhookDelivery:UseHttpClient"] = "false",
            ["Email:Provider"] = EmailProviderNames.AzureCommunicationServices,
        };

        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(data).Build();
        Mock<IWebHostEnvironment> env = new();
        env.Setup(e => e.EnvironmentName).Returns(Environments.Production);

        IReadOnlyList<string> errors = ArchLucidConfigurationRules.CollectErrors(configuration, env.Object);

        errors.Should()
            .Contain(e => e.Contains("Email:AzureCommunicationServicesEndpoint", StringComparison.Ordinal));
    }

    [Fact]
    public void CollectErrors_WhenProductionApiAndAcsEmailWithConfiguredEndpoint_does_not_add_acs_endpoint_error()
    {
        Dictionary<string, string?> data = new()
        {
            ["ArchLucid:StorageProvider"] = "InMemory",
            ["ArchLucidAuth:Mode"] = "JwtBearer",
            ["ArchLucidAuth:Authority"] = "https://login.example.com",
            ["Cors:AllowedOrigins:0"] = "https://ops.example.com",
            ["WebhookDelivery:UseHttpClient"] = "false",
            ["Email:Provider"] = EmailProviderNames.AzureCommunicationServices,
            ["Email:AzureCommunicationServicesEndpoint"] = "https://contoso.communication.azure.com/",
        };

        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(data).Build();
        Mock<IWebHostEnvironment> env = new();
        env.Setup(e => e.EnvironmentName).Returns(Environments.Production);

        IReadOnlyList<string> errors = ArchLucidConfigurationRules.CollectErrors(configuration, env.Object);

        errors.Should()
            .NotContain(e => e.Contains("Email:AzureCommunicationServicesEndpoint", StringComparison.Ordinal));
    }

    [Fact]
    public void CollectErrors_WhenProductionApiAndStripeBillingWithoutSecretKey_contains_error()
    {
        Dictionary<string, string?> data = new()
        {
            ["ArchLucid:StorageProvider"] = "InMemory",
            ["ArchLucidAuth:Mode"] = "JwtBearer",
            ["ArchLucidAuth:Authority"] = "https://login.example.com",
            ["Cors:AllowedOrigins:0"] = "https://ops.example.com",
            ["WebhookDelivery:UseHttpClient"] = "false",
            ["Billing:Provider"] = BillingProviderNames.Stripe,
        };

        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(data).Build();
        Mock<IWebHostEnvironment> env = new();
        env.Setup(e => e.EnvironmentName).Returns(Environments.Production);

        IReadOnlyList<string> errors = ArchLucidConfigurationRules.CollectErrors(configuration, env.Object);

        errors.Should().Contain(e => e.Contains("Billing:Stripe:SecretKey", StringComparison.Ordinal));
    }

    [Fact]
    public void CollectErrors_WhenProductionWorkerAndStripeBillingWithoutSecretKey_contains_error()
    {
        Dictionary<string, string?> data = new()
        {
            ["Hosting:Role"] = "Worker",
            ["ArchLucid:StorageProvider"] = "InMemory",
            ["ArchLucidAuth:Mode"] = "JwtBearer",
            ["ArchLucidAuth:Authority"] = "https://login.example.com",
            ["WebhookDelivery:UseHttpClient"] = "false",
            ["Billing:Provider"] = BillingProviderNames.Stripe,
        };

        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(data).Build();
        Mock<IWebHostEnvironment> env = new();
        env.Setup(e => e.EnvironmentName).Returns(Environments.Production);

        IReadOnlyList<string> errors = ArchLucidConfigurationRules.CollectErrors(configuration, env.Object);

        errors.Should().Contain(e => e.Contains("Billing:Stripe:SecretKey", StringComparison.Ordinal));
    }

    [Fact]
    public void CollectErrors_WhenProductionApiAndStripeBillingWithSecretKey_has_no_billing_secret_error()
    {
        Dictionary<string, string?> data = new()
        {
            ["ArchLucid:StorageProvider"] = "InMemory",
            ["ArchLucidAuth:Mode"] = "JwtBearer",
            ["ArchLucidAuth:Authority"] = "https://login.example.com",
            ["Cors:AllowedOrigins:0"] = "https://ops.example.com",
            ["WebhookDelivery:UseHttpClient"] = "false",
            ["Billing:Provider"] = BillingProviderNames.Stripe,
            // Intentionally not sk_test_/sk_live_ shaped — gitleaks flags those as real Stripe tokens.
            ["Billing:Stripe:SecretKey"] = "unit-test-keyvault-ref-stripe-secret-not-a-real-key",
        };

        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(data).Build();
        Mock<IWebHostEnvironment> env = new();
        env.Setup(e => e.EnvironmentName).Returns(Environments.Production);

        IReadOnlyList<string> errors = ArchLucidConfigurationRules.CollectErrors(configuration, env.Object);

        errors.Should().NotContain(e => e.Contains("Billing:Stripe:SecretKey", StringComparison.Ordinal));
    }
}
