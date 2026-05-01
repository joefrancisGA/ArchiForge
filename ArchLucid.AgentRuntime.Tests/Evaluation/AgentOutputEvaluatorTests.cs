using ArchLucid.AgentRuntime.Evaluation;
using ArchLucid.Contracts.Agents;
using ArchLucid.Contracts.Common;

using FluentAssertions;

namespace ArchLucid.AgentRuntime.Tests.Evaluation;

public sealed class AgentOutputEvaluatorTests
{
    private readonly AgentOutputEvaluator _sut = new();

    [SkippableFact]
    public void Evaluate_When_json_invalid_sets_parse_failure_and_zero_ratio()
    {
        AgentOutputEvaluationScore score = _sut.Evaluate("t1", "{not-json", AgentType.Topology);

        score.IsJsonParseFailure.Should().BeTrue();
        score.StructuralCompletenessRatio.Should().Be(0.0);
        score.MissingKeys.Should().NotBeEmpty();
    }

    [SkippableFact]
    public void Evaluate_When_root_is_array_sets_parse_failure()
    {
        AgentOutputEvaluationScore score = _sut.Evaluate("t1", "[1,2]", AgentType.Cost);

        score.IsJsonParseFailure.Should().BeTrue();
    }

    [SkippableFact]
    public void Evaluate_When_all_expected_keys_present_ratio_is_one()
    {
        const string json =
            """
            {"resultId":"a","taskId":"b","runId":"c","agentType":1,"claims":[],"evidenceRefs":[],"confidence":0.5,"findings":[],"proposedChanges":null,"createdUtc":"2026-01-01T00:00:00Z"}
            """;

        AgentOutputEvaluationScore score = _sut.Evaluate("t1", json, AgentType.Topology);

        score.IsJsonParseFailure.Should().BeFalse();
        score.StructuralCompletenessRatio.Should().Be(1.0);
        score.MissingKeys.Should().BeEmpty();
    }

    [SkippableFact]
    public void Evaluate_When_empty_object_counts_all_keys_missing_without_parse_failure()
    {
        AgentOutputEvaluationScore score = _sut.Evaluate("t1", "{}", AgentType.Compliance);

        score.IsJsonParseFailure.Should().BeFalse();
        score.StructuralCompletenessRatio.Should().Be(0.0);
        score.MissingKeys.Should().HaveCount(10);
    }

    [SkippableFact]
    public void Evaluate_When_partial_keys_ratio_matches_expected_fraction()
    {
        const string json = """{"claims":[],"evidenceRefs":[],"confidence":0.3,"findings":[],"proposedChanges":null}""";

        AgentOutputEvaluationScore score = _sut.Evaluate("t1", json, AgentType.Critic);

        score.IsJsonParseFailure.Should().BeFalse();
        score.StructuralCompletenessRatio.Should().BeApproximately(5.0 / 10.0, 0.0001);
        score.MissingKeys.Should().HaveCount(5);
    }

    [SkippableFact]
    public void Evaluate_When_parsed_json_null_scores_zero_without_parse_failure_flag()
    {
        AgentOutputEvaluationScore score = _sut.Evaluate("t1", null, AgentType.Topology);

        score.IsJsonParseFailure.Should().BeFalse();
        score.StructuralCompletenessRatio.Should().Be(0.0);
        score.MissingKeys.Should().HaveCount(10);
    }
}
