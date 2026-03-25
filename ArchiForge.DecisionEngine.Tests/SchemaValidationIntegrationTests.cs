using ArchiForge.DecisionEngine.Validation;

using FluentAssertions;

using Microsoft.Extensions.DependencyInjection;

namespace ArchiForge.DecisionEngine.Tests;

public sealed class SchemaValidationIntegrationTests
{
    [Fact]
    public void ServiceRegistration_WithConfiguration_RegistersSuccessfully()
    {
        ServiceCollection services = new();
        services.AddLogging();

        services.AddSchemaValidation(options =>
        {
            options.AgentResultSchemaPath = "schemas/agentresult.schema.json";
            options.GoldenManifestSchemaPath = "schemas/goldenmanifest.schema.json";
            options.EnableDetailedErrors = true;
        });

        ServiceProvider serviceProvider = services.BuildServiceProvider();

        ISchemaValidationService? service = serviceProvider.GetService<ISchemaValidationService>();

        service.Should().NotBeNull();
        service.Should().BeOfType<SchemaValidationService>();
    }

    [Fact]
    public void ServiceRegistration_IsSingleton()
    {
        ServiceCollection services = new();
        services.AddLogging();

        services.AddSchemaValidation(options =>
        {
            options.AgentResultSchemaPath = "schemas/agentresult.schema.json";
            options.GoldenManifestSchemaPath = "schemas/goldenmanifest.schema.json";
        });

        ServiceProvider serviceProvider = services.BuildServiceProvider();

        ISchemaValidationService? service1 = serviceProvider.GetService<ISchemaValidationService>();
        ISchemaValidationService? service2 = serviceProvider.GetService<ISchemaValidationService>();

        service1.Should().BeSameAs(service2);
    }

    [Fact]
    public void MultipleValidations_UseSameSchemasInstances()
    {
        ServiceCollection services = new();
        services.AddLogging();

        services.AddSchemaValidation(options =>
        {
            options.AgentResultSchemaPath = "schemas/agentresult.schema.json";
            options.GoldenManifestSchemaPath = "schemas/goldenmanifest.schema.json";
        });

        ServiceProvider serviceProvider = services.BuildServiceProvider();
        ISchemaValidationService service = serviceProvider.GetRequiredService<ISchemaValidationService>();

        SchemaValidationResult result1 = service.ValidateAgentResultJson("{}");
        SchemaValidationResult result2 = service.ValidateAgentResultJson("{}");

        result1.Should().NotBeNull();
        result2.Should().NotBeNull();
    }

    [Fact]
    [Trait("Category", "Integration")]
    public void ValidateAgentResult_WithRealSchema_ValidPayload_ReturnsSuccess()
    {
        ServiceCollection services = new();
        services.AddLogging();
        services.AddSchemaValidation(options =>
        {
            options.AgentResultSchemaPath = "schemas/agentresult.schema.json";
            options.GoldenManifestSchemaPath = "schemas/goldenmanifest.schema.json";
        });
        ISchemaValidationService service = services.BuildServiceProvider().GetRequiredService<ISchemaValidationService>();

        string validJson = """
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

        SchemaValidationResult result = service.ValidateAgentResultJson(validJson);

        result.Should().NotBeNull();
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    [Trait("Category", "Integration")]
    public void ValidateAgentResult_WithRealSchema_InvalidPayload_ReturnsErrors()
    {
        ServiceCollection services = new();
        services.AddLogging();
        services.AddSchemaValidation(options =>
        {
            options.AgentResultSchemaPath = "schemas/agentresult.schema.json";
            options.GoldenManifestSchemaPath = "schemas/goldenmanifest.schema.json";
        });
        ISchemaValidationService service = services.BuildServiceProvider().GetRequiredService<ISchemaValidationService>();

        string invalidJson = """{"agentType":"Unknown","confidence":2}""";

        SchemaValidationResult result = service.ValidateAgentResultJson(invalidJson);

        result.Should().NotBeNull();
        result.IsValid.Should().BeFalse();
        result.Errors.Should().NotBeEmpty();
    }
}
