using ArchiForge.Contracts.Agents;
using ArchiForge.Contracts.Common;

using FluentAssertions;

namespace ArchiForge.AgentRuntime.Tests;

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
            {"runId":"other","taskId":"task","agentType":"Topology","claims":[],"evidenceRefs":[],"confidence":0.5}
            """;

        Action act = () => _sut.ParseAndValidate(json, "run", "task", AgentType.Topology);

        act.Should().Throw<InvalidOperationException>().WithMessage("*RunId*");
    }

    [Fact]
    public void ParseAndValidate_when_valid_returns_result()
    {
        string json =
            """
            {"runId":"run1","taskId":"task1","agentType":"Topology","resultId":"res1","claims":["c"],"evidenceRefs":["e"],"confidence":0.75}
            """;

        AgentResult result = _sut.ParseAndValidate(json, "run1", "task1", AgentType.Topology);

        result.ResultId.Should().Be("res1");
        result.Confidence.Should().Be(0.75);
        result.Claims.Should().ContainSingle().Which.Should().Be("c");
    }

    [Fact]
    public void ParseAndValidate_when_confidence_out_of_range_throws()
    {
        string json =
            """
            {"runId":"r","taskId":"t","agentType":"Compliance","resultId":"x","claims":[],"evidenceRefs":[],"confidence":2}
            """;

        Action act = () => _sut.ParseAndValidate(json, "r", "t", AgentType.Compliance);

        act.Should().Throw<InvalidOperationException>().WithMessage("*Confidence*");
    }
}
