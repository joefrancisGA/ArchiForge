using ArchLucid.Contracts.Agents;
using ArchLucid.Contracts.Common;
using ArchLucid.Decisioning.Validation;

using FluentAssertions;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Moq;

namespace ArchLucid.AgentRuntime.Tests;

[Trait("Category", "Unit")]
[Trait("Suite", "Core")]
public sealed class AgentResultParserTests
{
    private readonly AgentResultParser _sut = new();

    [Fact]
    public void ParseAndValidate_when_json_empty_throws()
    {
        Action act = () => _sut.ParseAndValidate("  ", "run", "task", AgentType.Topology);

        act.Should().Throw<InvalidOperationException>().WithMessage("*empty*");
    }

    [Fact]
    public void ParseAndValidate_when_json_invalid_throws()
    {
        Action act = () => _sut.ParseAndValidate("{", "run", "task", AgentType.Topology);

        act.Should().Throw<InvalidOperationException>().WithMessage("*deserialize*");
    }

    [Fact]
    public void ParseAndValidate_when_ids_mismatch_throws()
    {
        string json =
            """
            {"runId":"other","taskId":"task","agentType":"Topology","resultId":"r","claims":[],"evidenceRefs":[],"confidence":0.5,"createdUtc":"2026-01-01T00:00:00Z"}
            """;

        Action act = () => _sut.ParseAndValidate(json, "run", "task", AgentType.Topology);

        act.Should().Throw<InvalidOperationException>().WithMessage("*RunId*");
    }

    [Fact]
    public void ParseAndValidate_when_valid_returns_result()
    {
        string json =
            """
            {"runId":"run1","taskId":"task1","agentType":"Topology","resultId":"res1","claims":["c"],"evidenceRefs":["e"],"confidence":0.75,"createdUtc":"2026-01-01T00:00:00Z"}
            """;

        AgentResult result = _sut.ParseAndValidate(json, "run1", "task1", AgentType.Topology);

        result.ResultId.Should().Be("res1");
        result.Confidence.Should().Be(0.75);
        result.Claims.Should().ContainSingle().Which.Should().Be("c");
    }

    [Fact]
    public void ParseAndValidate_when_confidence_out_of_range_throws_InvalidOperationException()
    {
        string json =
            """
            {"runId":"r","taskId":"t","agentType":"Compliance","resultId":"x","claims":[],"evidenceRefs":[],"confidence":2,"createdUtc":"2026-01-01T00:00:00Z"}
            """;

        Action act = () => _sut.ParseAndValidate(json, "r", "t", AgentType.Compliance);

        act.Should().Throw<InvalidOperationException>().WithMessage("*between 0 and 1*");
    }

    [Fact]
    public void ParseAndValidate_when_schema_invalid_and_enforce_on_throws_SchemaViolationException()
    {
        Mock<ILogger<AgentResultParser>> logger = new();
        SchemaValidationService schema = CreateSchemaService();
        AgentResultParser sut = new(
            schema,
            Options.Create(new AgentResultSchemaValidationOptions { EnforceOnParse = true }),
            logger.Object);

        string json =
            """
            {"runId":"r","taskId":"t","agentType":"Topology","resultId":"x","evidenceRefs":[],"confidence":0.5,"createdUtc":"2026-01-01T00:00:00Z"}
            """;

        Action act = () => sut.ParseAndValidate(json, "r", "t", AgentType.Topology);

        act.Should().Throw<AgentResultSchemaViolationException>()
            .Which.SchemaErrors.Should().NotBeEmpty();
    }

    [Fact]
    public void ParseAndValidate_when_schema_invalid_and_enforce_off_returns_result_and_logs_warning()
    {
        Mock<ILogger<AgentResultParser>> logger = new();
        logger.Setup(l => l.IsEnabled(LogLevel.Warning)).Returns(true);
        SchemaValidationService schema = CreateSchemaService();
        AgentResultParser sut = new(
            schema,
            Options.Create(new AgentResultSchemaValidationOptions { EnforceOnParse = false }),
            logger.Object);

        string json =
            """
            {"runId":"r","taskId":"t","agentType":"Topology","resultId":"x","evidenceRefs":[],"confidence":0.5,"createdUtc":"2026-01-01T00:00:00Z"}
            """;

        AgentResult result = sut.ParseAndValidate(json, "r", "t", AgentType.Topology);

        result.ResultId.Should().Be("x");
        logger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, _) => o.ToString()!.Contains("EnforceOnParse", StringComparison.Ordinal)),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public void ParseAndValidate_valid_json_passes_schema_and_returns_result()
    {
        SchemaValidationService schema = CreateSchemaService();
        AgentResultParser sut = new(
            schema,
            Options.Create(new AgentResultSchemaValidationOptions()),
            Mock.Of<ILogger<AgentResultParser>>());

        string json =
            """
            {"runId":"r1","taskId":"t1","agentType":"Topology","resultId":"rid","claims":["a"],"evidenceRefs":["b"],"confidence":0.5,"createdUtc":"2026-01-01T00:00:00Z"}
            """;

        AgentResult result = sut.ParseAndValidate(json, "r1", "t1", AgentType.Topology);

        result.Claims.Should().ContainSingle();
    }

    private static SchemaValidationService CreateSchemaService()
    {
        Mock<ILogger<SchemaValidationService>> log = new();
        SchemaValidationOptions options = new()
        {
            AgentResultSchemaPath = "schemas/agentresult.schema.json",
            GoldenManifestSchemaPath = "schemas/goldenmanifest.schema.json",
            ExplanationRunSchemaPath = "schemas/explanation-run.schema.json",
            ComparisonExplanationSchemaPath = "schemas/comparison-explanation.schema.json",
        };

        return new SchemaValidationService(log.Object, Options.Create(options));
    }
}
