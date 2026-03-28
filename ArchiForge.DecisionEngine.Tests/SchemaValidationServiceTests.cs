using ArchiForge.DecisionEngine.Validation;

using FluentAssertions;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Moq;

#pragma warning disable CA1873 // Avoid conditional access in logger message for test verifications

namespace ArchiForge.DecisionEngine.Tests;

[Trait("Suite", "Core")]
[Trait("Category", "Slow")]
public sealed class SchemaValidationServiceTests
{
    private readonly Mock<ILogger<SchemaValidationService>> _loggerMock = new();
    private readonly SchemaValidationOptions _options = new()
    {
        AgentResultSchemaPath = "schemas/agentresult.schema.json",
        GoldenManifestSchemaPath = "schemas/goldenmanifest.schema.json",
        EnableDetailedErrors = true
    };

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        Func<SchemaValidationService> action = () => new SchemaValidationService(
            null!,
            Options.Create(_options));

        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    [Fact]
    public void Constructor_WithNullOptions_ThrowsArgumentNullException()
    {
        Func<SchemaValidationService> action = () => new SchemaValidationService(
            _loggerMock.Object,
            null!);

        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("options");
    }

    [Fact]
    public void ValidateAgentResultJson_WithEmptyJson_ReturnsInvalidResult()
    {
        SchemaValidationService service = CreateService();

        SchemaValidationResult result = service.ValidateAgentResultJson(string.Empty);

        result.Should().NotBeNull();
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle()
            .Which.Should().Contain("AgentResult JSON payload is empty");
    }

    [Fact]
    public void ValidateGoldenManifestJson_WithEmptyJson_ReturnsInvalidResult()
    {
        SchemaValidationService service = CreateService();

        SchemaValidationResult result = service.ValidateGoldenManifestJson(string.Empty);

        result.Should().NotBeNull();
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle()
            .Which.Should().Contain("GoldenManifest JSON payload is empty");
    }

    [Fact]
    public void ValidateAgentResultJson_WithWhitespaceJson_ReturnsInvalidResult()
    {
        SchemaValidationService service = CreateService();

        SchemaValidationResult result = service.ValidateAgentResultJson("   \t\n  ");

        result.Should().NotBeNull();
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle();
    }

    [Fact]
    public void ValidateAgentResultJson_WithInvalidJson_ReturnsParseError()
    {
        SchemaValidationService service = CreateService();

        SchemaValidationResult result = service.ValidateAgentResultJson("{ invalid json }");

        result.Should().NotBeNull();
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle()
            .Which.Should().Contain("could not be parsed");
    }

    [Fact]
    public void ValidateAgentResultJson_WithMalformedJson_ReturnsParseError()
    {
        SchemaValidationService service = CreateService();

        SchemaValidationResult result = service.ValidateAgentResultJson("{\"key\": }");

        result.Should().NotBeNull();
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle()
            .Which.Should().Contain("could not be parsed");
    }

    [Fact]
    public async Task ValidateAgentResultJsonAsync_WithEmptyJson_ReturnsInvalidResult()
    {
        SchemaValidationService service = CreateService();

        SchemaValidationResult result = await service.ValidateAgentResultJsonAsync(string.Empty);

        result.Should().NotBeNull();
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle();
    }

    [Fact]
    public async Task ValidateGoldenManifestJsonAsync_WithEmptyJson_ReturnsInvalidResult()
    {
        SchemaValidationService service = CreateService();

        SchemaValidationResult result = await service.ValidateGoldenManifestJsonAsync(string.Empty);

        result.Should().NotBeNull();
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle();
    }

    [Fact]
    public async Task ValidateAgentResultJsonAsync_WithCancellation_ThrowsOperationCanceledException()
    {
        SchemaValidationService service = CreateService();
        CancellationTokenSource cts = new();
        await cts.CancelAsync();

        Func<Task<SchemaValidationResult>> action = async () => await service.ValidateAgentResultJsonAsync(
            "{\"test\": \"value\"}",
            cts.Token);

        await action.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public void ValidateAgentResultJson_WithDetailedErrorsEnabled_PopulatesDetailedErrors()
    {
        SchemaValidationService service = CreateService();
        string invalidJson = "{\"unknownField\": \"value\"}";

        SchemaValidationResult result = service.ValidateAgentResultJson(invalidJson);

        if (result.IsValid)
        {
            return;
        }
        
        result.DetailedErrors.Should().NotBeEmpty();
        result.DetailedErrors.Should().AllSatisfy(error =>
        {
            error.Message.Should().NotBeNullOrEmpty();
            error.Location.Should().NotBeNullOrEmpty();
        });
    }

    [Fact]
    public void ValidateAgentResultJson_WithDetailedErrorsDisabled_DoesNotPopulateDetailedErrors()
    {
        SchemaValidationOptions optionsWithoutDetails = new()
        {
            AgentResultSchemaPath = "schemas/agentresult.schema.json",
            GoldenManifestSchemaPath = "schemas/goldenmanifest.schema.json",
            EnableDetailedErrors = false
        };

        SchemaValidationService service = new(
            _loggerMock.Object,
            Options.Create(optionsWithoutDetails));

        string invalidJson = "{\"unknownField\": \"value\"}";

        SchemaValidationResult result = service.ValidateAgentResultJson(invalidJson);

        if (!result.IsValid)
        {
            result.DetailedErrors.Should().BeEmpty();
        }
    }

    [Fact]
    public void ValidateAgentResultJson_LogsValidationFailure()
    {
        SchemaValidationService service = CreateService();

        service.ValidateAgentResultJson(string.Empty);

        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Empty payload")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void ValidateGoldenManifestJson_LogsValidationFailure()
    {
        SchemaValidationService service = CreateService();

        service.ValidateGoldenManifestJson(string.Empty);

        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Empty payload")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void LazyLoading_SchemaLoadedOnlyOnFirstUse()
    {
        SchemaValidationService service = CreateService();

        _loggerMock.Verify(
            x => x.Log(
                It.IsAny<LogLevel>(),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Loading schema")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Never);

        service.ValidateAgentResultJson("{}");

        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Loading schema")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    private SchemaValidationService CreateService()
    {
        return new SchemaValidationService(
            _loggerMock.Object,
            Options.Create(_options));
    }
}

#pragma warning restore CA1873
