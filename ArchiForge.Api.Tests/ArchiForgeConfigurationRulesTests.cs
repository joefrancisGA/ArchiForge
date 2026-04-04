using ArchiForge.Host.Core.Startup.Validation;

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
            ["Cors:AllowedOrigins:0"] = "https://ops.example.com",
            ["WebhookDelivery:UseHttpClient"] = "false",
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
            ["Cors:AllowedOrigins:0"] = "https://ops.example.com",
            ["WebhookDelivery:UseHttpClient"] = "false",
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
            ["Cors:AllowedOrigins:0"] = "https://ops.example.com",
            ["WebhookDelivery:UseHttpClient"] = "false",
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
    public void CollectErrors_WhenRealModeAndMaxCompletionTokensNegative_contains_error()
    {
        Dictionary<string, string?> data = new()
        {
            ["ArchiForge:StorageProvider"] = "InMemory",
            ["ArchiForgeAuth:Mode"] = "DevelopmentBypass",
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

        IReadOnlyList<string> errors = ArchiForgeConfigurationRules.CollectErrors(configuration, env.Object);

        errors.Should().Contain(e => e.Contains("MaxCompletionTokens", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void CollectErrors_WhenLlmCompletionCacheMaxEntriesZero_contains_error()
    {
        Dictionary<string, string?> data = new()
        {
            ["ArchiForge:StorageProvider"] = "InMemory",
            ["ArchiForgeAuth:Mode"] = "DevelopmentBypass",
            ["LlmCompletionCache:Enabled"] = "true",
            ["LlmCompletionCache:MaxEntries"] = "0",
            ["Cors:AllowedOrigins:0"] = "https://ops.example.com",
            ["WebhookDelivery:UseHttpClient"] = "false",
        };

        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(data).Build();
        Mock<IWebHostEnvironment> env = new();
        env.Setup(e => e.EnvironmentName).Returns(Environments.Development);

        IReadOnlyList<string> errors = ArchiForgeConfigurationRules.CollectErrors(configuration, env.Object);

        errors.Should().Contain(e => e.Contains("LlmCompletionCache:MaxEntries", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void CollectErrors_WhenDurableJobsAndProcessorReceiveBatchSizeInvalid_contains_error()
    {
        Dictionary<string, string?> data = new()
        {
            ["ArchiForge:StorageProvider"] = "Sql",
            ["ArchiForgeAuth:Mode"] = "DevelopmentBypass",
            ["ConnectionStrings:ArchiForge"] = "Server=.;Database=x;Trusted_Connection=True;TrustServerCertificate=True",
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

        IReadOnlyList<string> errors = ArchiForgeConfigurationRules.CollectErrors(configuration, env.Object);

        errors.Should().Contain(e => e.Contains("ProcessorReceiveBatchSize", StringComparison.OrdinalIgnoreCase));
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

    [Fact]
    public void CollectErrors_WhenDataArchivalIntervalInvalid_contains_error()
    {
        Dictionary<string, string?> data = new()
        {
            ["ArchiForge:StorageProvider"] = "InMemory",
            ["ArchiForgeAuth:Mode"] = "DevelopmentBypass",
            ["DataArchival:IntervalHours"] = "0",
        };

        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(data).Build();
        Mock<IWebHostEnvironment> env = new();
        env.Setup(e => e.EnvironmentName).Returns(Environments.Development);

        IReadOnlyList<string> errors = ArchiForgeConfigurationRules.CollectErrors(configuration, env.Object);

        errors.Should().Contain(e => e.Contains("DataArchival:IntervalHours", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void CollectErrors_WhenRateLimitingFixedWindowPermitLimitNegative_contains_error()
    {
        Dictionary<string, string?> data = new()
        {
            ["ArchiForge:StorageProvider"] = "InMemory",
            ["ArchiForgeAuth:Mode"] = "DevelopmentBypass",
            ["RateLimiting:FixedWindow:PermitLimit"] = "-1",
        };

        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(data).Build();
        Mock<IWebHostEnvironment> env = new();
        env.Setup(e => e.EnvironmentName).Returns(Environments.Development);

        IReadOnlyList<string> errors = ArchiForgeConfigurationRules.CollectErrors(configuration, env.Object);

        errors.Should().Contain(e => e.Contains("RateLimiting:FixedWindow", StringComparison.OrdinalIgnoreCase)
            && e.Contains("PermitLimit", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void CollectErrors_WhenRateLimitingExpensiveWindowMinutesZero_contains_error()
    {
        Dictionary<string, string?> data = new()
        {
            ["ArchiForge:StorageProvider"] = "InMemory",
            ["ArchiForgeAuth:Mode"] = "DevelopmentBypass",
            ["RateLimiting:Expensive:WindowMinutes"] = "0",
        };

        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(data).Build();
        Mock<IWebHostEnvironment> env = new();
        env.Setup(e => e.EnvironmentName).Returns(Environments.Development);

        IReadOnlyList<string> errors = ArchiForgeConfigurationRules.CollectErrors(configuration, env.Object);

        errors.Should().Contain(e => e.Contains("RateLimiting:Expensive", StringComparison.OrdinalIgnoreCase)
            && e.Contains("WindowMinutes", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void CollectErrors_WhenProductionAndCorsOriginsEmpty_contains_error()
    {
        Dictionary<string, string?> data = new()
        {
            ["ArchiForge:StorageProvider"] = "InMemory",
            ["ArchiForgeAuth:Mode"] = "JwtBearer",
            ["ArchiForgeAuth:Authority"] = "https://login.example.com",
            ["WebhookDelivery:UseHttpClient"] = "false",
        };

        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(data).Build();
        Mock<IWebHostEnvironment> env = new();
        env.Setup(e => e.EnvironmentName).Returns(Environments.Production);

        IReadOnlyList<string> errors = ArchiForgeConfigurationRules.CollectErrors(configuration, env.Object);

        errors.Should().Contain(e => e.Contains("Cors:AllowedOrigins", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void CollectErrors_WhenProductionAndHostingRoleWorker_does_not_require_cors_origins()
    {
        Dictionary<string, string?> data = new()
        {
            ["Hosting:Role"] = "Worker",
            ["ArchiForge:StorageProvider"] = "InMemory",
            ["ArchiForgeAuth:Mode"] = "JwtBearer",
            ["ArchiForgeAuth:Authority"] = "https://login.example.com",
            ["WebhookDelivery:UseHttpClient"] = "false",
        };

        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(data).Build();
        Mock<IWebHostEnvironment> env = new();
        env.Setup(e => e.EnvironmentName).Returns(Environments.Production);

        IReadOnlyList<string> errors = ArchiForgeConfigurationRules.CollectErrors(configuration, env.Object);

        errors.Should().NotContain(e => e.Contains("Cors:AllowedOrigins", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void CollectErrors_WhenProductionAndCorsWildcard_contains_error()
    {
        Dictionary<string, string?> data = new()
        {
            ["ArchiForge:StorageProvider"] = "InMemory",
            ["ArchiForgeAuth:Mode"] = "JwtBearer",
            ["ArchiForgeAuth:Authority"] = "https://login.example.com",
            ["Cors:AllowedOrigins:0"] = "*",
            ["WebhookDelivery:UseHttpClient"] = "false",
        };

        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(data).Build();
        Mock<IWebHostEnvironment> env = new();
        env.Setup(e => e.EnvironmentName).Returns(Environments.Production);

        IReadOnlyList<string> errors = ArchiForgeConfigurationRules.CollectErrors(configuration, env.Object);

        errors.Should().Contain(e => e.Contains("wildcard", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void CollectErrors_WhenProductionAndWebhookHttpWithoutSecret_contains_error()
    {
        Dictionary<string, string?> data = new()
        {
            ["ArchiForge:StorageProvider"] = "InMemory",
            ["ArchiForgeAuth:Mode"] = "JwtBearer",
            ["ArchiForgeAuth:Authority"] = "https://login.example.com",
            ["Cors:AllowedOrigins:0"] = "https://ops.example.com",
            ["WebhookDelivery:UseHttpClient"] = "true",
            ["WebhookDelivery:HmacSha256SharedSecret"] = "",
        };

        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(data).Build();
        Mock<IWebHostEnvironment> env = new();
        env.Setup(e => e.EnvironmentName).Returns(Environments.Production);

        IReadOnlyList<string> errors = ArchiForgeConfigurationRules.CollectErrors(configuration, env.Object);

        errors.Should().Contain(e => e.Contains("WebhookDelivery:HmacSha256SharedSecret", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void CollectErrors_WhenRetrievalVectorIndexInvalid_contains_error()
    {
        Dictionary<string, string?> data = new()
        {
            ["ArchiForge:StorageProvider"] = "InMemory",
            ["ArchiForgeAuth:Mode"] = "DevelopmentBypass",
            ["Retrieval:VectorIndex"] = "Elastic",
        };

        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(data).Build();
        Mock<IWebHostEnvironment> env = new();
        env.Setup(e => e.EnvironmentName).Returns(Environments.Development);

        IReadOnlyList<string> errors = ArchiForgeConfigurationRules.CollectErrors(configuration, env.Object);

        errors.Should().Contain(e => e.Contains("Retrieval:VectorIndex", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void CollectErrors_WhenProductionAndWebhookSecretTooShort_contains_error()
    {
        Dictionary<string, string?> data = new()
        {
            ["ArchiForge:StorageProvider"] = "InMemory",
            ["ArchiForgeAuth:Mode"] = "JwtBearer",
            ["ArchiForgeAuth:Authority"] = "https://login.example.com",
            ["Cors:AllowedOrigins:0"] = "https://ops.example.com",
            ["WebhookDelivery:UseHttpClient"] = "true",
            ["WebhookDelivery:HmacSha256SharedSecret"] = "short-secret-not-32-chars",
        };

        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(data).Build();
        Mock<IWebHostEnvironment> env = new();
        env.Setup(e => e.EnvironmentName).Returns(Environments.Production);

        IReadOnlyList<string> errors = ArchiForgeConfigurationRules.CollectErrors(configuration, env.Object);

        errors.Should().Contain(e =>
            e.Contains("WebhookDelivery:HmacSha256SharedSecret", StringComparison.OrdinalIgnoreCase) &&
            e.Contains("32", StringComparison.Ordinal));
    }

    [Fact]
    public void CollectErrors_WhenRateLimitingReplayLightQueueLimitNegative_contains_error()
    {
        Dictionary<string, string?> data = new()
        {
            ["ArchiForge:StorageProvider"] = "InMemory",
            ["ArchiForgeAuth:Mode"] = "DevelopmentBypass",
            ["RateLimiting:Replay:Light:PermitLimit"] = "10",
            ["RateLimiting:Replay:Light:WindowMinutes"] = "1",
            ["RateLimiting:Replay:Light:QueueLimit"] = "-1",
        };

        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(data).Build();
        Mock<IWebHostEnvironment> env = new();
        env.Setup(e => e.EnvironmentName).Returns(Environments.Development);

        IReadOnlyList<string> errors = ArchiForgeConfigurationRules.CollectErrors(configuration, env.Object);

        errors.Should().Contain(e => e.Contains("RateLimiting:Replay:Light", StringComparison.OrdinalIgnoreCase)
            && e.Contains("QueueLimit", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void CollectErrors_WhenApiKeyEnabledButNoKeysConfigured_contains_error()
    {
        Dictionary<string, string?> data = new()
        {
            ["ArchiForge:StorageProvider"] = "InMemory",
            ["ArchiForgeAuth:Mode"] = "DevelopmentBypass",
            ["Authentication:ApiKey:Enabled"] = "true",
            ["Authentication:ApiKey:AdminKey"] = "",
            ["Authentication:ApiKey:ReadOnlyKey"] = "",
        };

        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(data).Build();
        Mock<IWebHostEnvironment> env = new();
        env.Setup(e => e.EnvironmentName).Returns(Environments.Development);

        IReadOnlyList<string> errors = ArchiForgeConfigurationRules.CollectErrors(configuration, env.Object);

        errors.Should().Contain(e => e.Contains("Authentication:ApiKey:Enabled is true", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void CollectErrors_WhenRetrievalEmbeddingCapsInvalid_contains_errors()
    {
        Dictionary<string, string?> data = new()
        {
            ["ArchiForge:StorageProvider"] = "InMemory",
            ["ArchiForgeAuth:Mode"] = "DevelopmentBypass",
            ["Retrieval:EmbeddingCaps:MaxTextsPerEmbeddingRequest"] = "0",
            ["Retrieval:EmbeddingCaps:MaxChunksPerIndexOperation"] = "2000000",
        };

        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(data).Build();
        Mock<IWebHostEnvironment> env = new();
        env.Setup(e => e.EnvironmentName).Returns(Environments.Development);

        IReadOnlyList<string> errors = ArchiForgeConfigurationRules.CollectErrors(configuration, env.Object);

        errors.Should().Contain(e => e.Contains("Retrieval:EmbeddingCaps:MaxTextsPerEmbeddingRequest", StringComparison.OrdinalIgnoreCase));
        errors.Should().Contain(e => e.Contains("Retrieval:EmbeddingCaps:MaxChunksPerIndexOperation", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void CollectErrors_WhenDataArchivalRunsRetentionOutOfRange_contains_error()
    {
        Dictionary<string, string?> data = new()
        {
            ["ArchiForge:StorageProvider"] = "InMemory",
            ["ArchiForgeAuth:Mode"] = "DevelopmentBypass",
            ["DataArchival:RunsRetentionDays"] = "-5",
        };

        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(data).Build();
        Mock<IWebHostEnvironment> env = new();
        env.Setup(e => e.EnvironmentName).Returns(Environments.Development);

        IReadOnlyList<string> errors = ArchiForgeConfigurationRules.CollectErrors(configuration, env.Object);

        errors.Should().Contain(e => e.Contains("DataArchival:RunsRetentionDays", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void CollectErrors_WhenBatchReplayMaxIdsAbove500_contains_error()
    {
        Dictionary<string, string?> data = new()
        {
            ["ArchiForge:StorageProvider"] = "InMemory",
            ["ArchiForgeAuth:Mode"] = "DevelopmentBypass",
            ["ComparisonReplay:Batch:MaxComparisonRecordIds"] = "501",
        };

        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(data).Build();
        Mock<IWebHostEnvironment> env = new();
        env.Setup(e => e.EnvironmentName).Returns(Environments.Development);

        IReadOnlyList<string> errors = ArchiForgeConfigurationRules.CollectErrors(configuration, env.Object);

        errors.Should().Contain(e => e.Contains("ComparisonReplay:Batch:MaxComparisonRecordIds", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void CollectErrors_WhenSchemaPathEmpty_contains_error()
    {
        Dictionary<string, string?> data = new()
        {
            ["ArchiForge:StorageProvider"] = "InMemory",
            ["ArchiForgeAuth:Mode"] = "DevelopmentBypass",
            ["SchemaValidation:AgentResultSchemaPath"] = "   ",
        };

        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(data).Build();
        Mock<IWebHostEnvironment> env = new();
        env.Setup(e => e.EnvironmentName).Returns(Environments.Development);

        IReadOnlyList<string> errors = ArchiForgeConfigurationRules.CollectErrors(configuration, env.Object);

        errors.Should().Contain(e => e.Contains("AgentResult", StringComparison.OrdinalIgnoreCase)
            && e.Contains("missing", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void CollectErrors_WhenProductionApiKeyModeButBothKeysMissing_contains_error()
    {
        Dictionary<string, string?> data = new()
        {
            ["ArchiForge:StorageProvider"] = "InMemory",
            ["ArchiForgeAuth:Mode"] = "ApiKey",
            ["Authentication:ApiKey:Enabled"] = "true",
            ["Authentication:ApiKey:AdminKey"] = "",
            ["Authentication:ApiKey:ReadOnlyKey"] = "",
            ["Cors:AllowedOrigins:0"] = "https://ops.example.com",
            ["WebhookDelivery:UseHttpClient"] = "false",
        };

        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(data).Build();
        Mock<IWebHostEnvironment> env = new();
        env.Setup(e => e.EnvironmentName).Returns(Environments.Production);

        IReadOnlyList<string> errors = ArchiForgeConfigurationRules.CollectErrors(configuration, env.Object);

        errors.Should().Contain(e => e.Contains("Production ApiKey auth requires", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void CollectErrors_WhenHotPathCacheEnabledWithInvalidProvider_contains_error()
    {
        Dictionary<string, string?> data = new()
        {
            ["ArchiForge:StorageProvider"] = "InMemory",
            ["ArchiForgeAuth:Mode"] = "DevelopmentBypass",
            ["HotPathCache:Enabled"] = "true",
            ["HotPathCache:Provider"] = "CosmosDb",
        };

        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(data).Build();
        Mock<IWebHostEnvironment> env = new();
        env.Setup(e => e.EnvironmentName).Returns(Environments.Development);

        IReadOnlyList<string> errors = ArchiForgeConfigurationRules.CollectErrors(configuration, env.Object);

        errors.Should().Contain(e => e.Contains("HotPathCache:Provider", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void CollectErrors_WhenHotPathCacheRedisWithoutConnectionString_contains_error()
    {
        Dictionary<string, string?> data = new()
        {
            ["ArchiForge:StorageProvider"] = "InMemory",
            ["ArchiForgeAuth:Mode"] = "DevelopmentBypass",
            ["HotPathCache:Enabled"] = "true",
            ["HotPathCache:Provider"] = "Redis",
            ["HotPathCache:RedisConnectionString"] = "",
        };

        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(data).Build();
        Mock<IWebHostEnvironment> env = new();
        env.Setup(e => e.EnvironmentName).Returns(Environments.Development);

        IReadOnlyList<string> errors = ArchiForgeConfigurationRules.CollectErrors(configuration, env.Object);

        errors.Should().Contain(e => e.Contains("HotPathCache:RedisConnectionString", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void CollectErrors_WhenHotPathCacheTtlAbove3600_contains_error()
    {
        Dictionary<string, string?> data = new()
        {
            ["ArchiForge:StorageProvider"] = "InMemory",
            ["ArchiForgeAuth:Mode"] = "DevelopmentBypass",
            ["HotPathCache:Enabled"] = "true",
            ["HotPathCache:AbsoluteExpirationSeconds"] = "4000",
        };

        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(data).Build();
        Mock<IWebHostEnvironment> env = new();
        env.Setup(e => e.EnvironmentName).Returns(Environments.Development);

        IReadOnlyList<string> errors = ArchiForgeConfigurationRules.CollectErrors(configuration, env.Object);

        errors.Should().Contain(e => e.Contains("HotPathCache:AbsoluteExpirationSeconds", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void CollectErrors_WhenHotPathCacheAutoMultiReplicaWithoutRedisInProduction_contains_error()
    {
        Dictionary<string, string?> data = new()
        {
            ["ArchiForge:StorageProvider"] = "InMemory",
            ["ArchiForgeAuth:Mode"] = "JwtBearer",
            ["ArchiForgeAuth:Authority"] = "https://login.example.com",
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

        IReadOnlyList<string> errors = ArchiForgeConfigurationRules.CollectErrors(configuration, env.Object);

        errors.Should().Contain(e => e.Contains("HotPathCache:RedisConnectionString", StringComparison.OrdinalIgnoreCase)
            && e.Contains("ExpectedApiReplicaCount", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void CollectErrors_WhenHotPathCacheAutoMultiReplicaWithoutRedisInDevelopment_does_not_add_replica_redis_error()
    {
        Dictionary<string, string?> data = new()
        {
            ["ArchiForge:StorageProvider"] = "InMemory",
            ["ArchiForgeAuth:Mode"] = "DevelopmentBypass",
            ["HotPathCache:Enabled"] = "true",
            ["HotPathCache:Provider"] = "Auto",
            ["HotPathCache:ExpectedApiReplicaCount"] = "5",
            ["HotPathCache:RedisConnectionString"] = "",
        };

        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(data).Build();
        Mock<IWebHostEnvironment> env = new();
        env.Setup(e => e.EnvironmentName).Returns(Environments.Development);

        IReadOnlyList<string> errors = ArchiForgeConfigurationRules.CollectErrors(configuration, env.Object);

        errors.Should().NotContain(e => e.Contains("greater than 1 outside Development", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void CollectErrors_WhenHostLeaderElectionRenewNotLessThanLease_contains_error()
    {
        Dictionary<string, string?> data = new()
        {
            ["ArchiForge:StorageProvider"] = "InMemory",
            ["ArchiForgeAuth:Mode"] = "DevelopmentBypass",
            ["HostLeaderElection:Enabled"] = "true",
            ["HostLeaderElection:LeaseDurationSeconds"] = "30",
            ["HostLeaderElection:RenewIntervalSeconds"] = "30",
        };

        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(data).Build();
        Mock<IWebHostEnvironment> env = new();
        env.Setup(e => e.EnvironmentName).Returns(Environments.Development);

        IReadOnlyList<string> errors = ArchiForgeConfigurationRules.CollectErrors(configuration, env.Object);

        errors.Should().Contain(e => e.Contains("HostLeaderElection:RenewIntervalSeconds", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void CollectErrors_WhenHostLeaderElectionDisabled_allows_renew_equal_to_lease_in_config()
    {
        Dictionary<string, string?> data = new()
        {
            ["ArchiForge:StorageProvider"] = "InMemory",
            ["ArchiForgeAuth:Mode"] = "DevelopmentBypass",
            ["HostLeaderElection:Enabled"] = "false",
            ["HostLeaderElection:LeaseDurationSeconds"] = "30",
            ["HostLeaderElection:RenewIntervalSeconds"] = "30",
        };

        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(data).Build();
        Mock<IWebHostEnvironment> env = new();
        env.Setup(e => e.EnvironmentName).Returns(Environments.Development);

        IReadOnlyList<string> errors = ArchiForgeConfigurationRules.CollectErrors(configuration, env.Object);

        errors.Should().NotContain(e => e.Contains("HostLeaderElection", StringComparison.OrdinalIgnoreCase));
    }
}
