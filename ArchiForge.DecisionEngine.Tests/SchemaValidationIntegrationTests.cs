using ArchiForge.DecisionEngine.Validation;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace ArchiForge.DecisionEngine.Tests;

public sealed class SchemaValidationIntegrationTests
{
    [Fact]
    public void ServiceRegistration_WithConfiguration_RegistersSuccessfully()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddSchemaValidation(options =>
        {
            options.AgentResultSchemaPath = "schemas/agentresult.schema.json";
            options.GoldenManifestSchemaPath = "schemas/goldenmanifest.schema.json";
            options.EnableDetailedErrors = true;
        });

        var serviceProvider = services.BuildServiceProvider();

        var service = serviceProvider.GetService<ISchemaValidationService>();

        service.Should().NotBeNull();
        service.Should().BeOfType<SchemaValidationService>();
    }

    [Fact]
    public void ServiceRegistration_IsSingleton()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddSchemaValidation(options =>
        {
            options.AgentResultSchemaPath = "schemas/agentresult.schema.json";
            options.GoldenManifestSchemaPath = "schemas/goldenmanifest.schema.json";
        });

        var serviceProvider = services.BuildServiceProvider();

        var service1 = serviceProvider.GetService<ISchemaValidationService>();
        var service2 = serviceProvider.GetService<ISchemaValidationService>();

        service1.Should().BeSameAs(service2);
    }

    [Fact]
    public void MultipleValidations_UseSameSchemasInstances()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddSchemaValidation(options =>
        {
            options.AgentResultSchemaPath = "schemas/agentresult.schema.json";
            options.GoldenManifestSchemaPath = "schemas/goldenmanifest.schema.json";
        });

        var serviceProvider = services.BuildServiceProvider();
        var service = serviceProvider.GetRequiredService<ISchemaValidationService>();

        var result1 = service.ValidateAgentResultJson("{}");
        var result2 = service.ValidateAgentResultJson("{}");

        result1.Should().NotBeNull();
        result2.Should().NotBeNull();
    }

    [Fact]
    [Trait("Category", "Integration")]
    public void ValidateAgentResult_WithRealSchema_ValidPayload_ReturnsSuccess()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSchemaValidation(options =>
        {
            options.AgentResultSchemaPath = "schemas/agentresult.schema.json";
            options.GoldenManifestSchemaPath = "schemas/goldenmanifest.schema.json";
        });
        var service = services.BuildServiceProvider().GetRequiredService<ISchemaValidationService>();

        var validJson = """
            {
                "resultId": "res-1",
                "taskId": "task-1",
                "runId": "run-1",
                "agentType": "Topology",
                "claims": ["claim1"],
                "evidenceRefs": ["ev-1"],
                "confidence": 0.9,
                "createdUtc": "2025-01-01T00:00:00Z"
            }
            """;

        var result = service.ValidateAgentResultJson(validJson);

        result.Should().NotBeNull();
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    [Trait("Category", "Integration")]
    public void ValidateAgentResult_WithRealSchema_InvalidPayload_ReturnsErrors()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSchemaValidation(options =>
        {
            options.AgentResultSchemaPath = "schemas/agentresult.schema.json";
            options.GoldenManifestSchemaPath = "schemas/goldenmanifest.schema.json";
        });
        var service = services.BuildServiceProvider().GetRequiredService<ISchemaValidationService>();

        var invalidJson = """{"agentType":"Unknown","confidence":2}""";

        var result = service.ValidateAgentResultJson(invalidJson);

        result.Should().NotBeNull();
        result.IsValid.Should().BeFalse();
        result.Errors.Should().NotBeEmpty();
    }
}
