using ArchLucid.AgentRuntime.Evaluation;
using ArchLucid.Contracts.Agents;
using ArchLucid.Contracts.Common;

using FluentAssertions;

namespace ArchLucid.AgentRuntime.Tests;

/// <summary>
///     Golden <see cref="AgentResult" />-shaped JSON for <see cref="AgentOutputEvaluator" /> /
///     <see cref="AgentOutputSemanticEvaluator" /> regression (see <c>docs/AGENT_OUTPUT_EVALUATION.md</c>).
/// </summary>
[Trait("Suite", "Core")]
[Trait("Category", "Unit")]
public sealed class GoldenAgentResultJsonEvaluationTests
{
    private const string TraceId = "trace-golden-eval";

    private static readonly AgentOutputEvaluator Structural = new();
    private static readonly AgentOutputSemanticEvaluator Semantic = new();

    [Fact]
    public void Golden_valid_fixture_scores_full_structural_completeness()
    {
        string json = LoadFixtureText("golden-agent-result-valid.json");

        AgentOutputEvaluationScore score = Structural.Evaluate(TraceId, json, AgentType.Topology);

        score.IsJsonParseFailure.Should().BeFalse();
        score.StructuralCompletenessRatio.Should().Be(1.0);
        score.MissingKeys.Should().BeEmpty();
    }

    [Fact]
    public void Golden_valid_fixture_has_nonzero_semantic_score_from_findings_and_claims()
    {
        string json = LoadFixtureText("golden-agent-result-valid.json");

        AgentOutputSemanticScore semantic = Semantic.Evaluate(TraceId, json, AgentType.Topology);

        semantic.OverallSemanticScore.Should().BeGreaterThan(0.0);
        semantic.FindingsQualityRatio.Should().Be(1.0);
    }

    [Fact]
    public void Golden_partial_fixture_is_incomplete_structurally_but_parseable()
    {
        string json = LoadFixtureText("golden-agent-result-partial-keys.json");

        AgentOutputEvaluationScore score = Structural.Evaluate(TraceId, json, AgentType.Topology);

        score.IsJsonParseFailure.Should().BeFalse();
        score.StructuralCompletenessRatio.Should().BeLessThan(1.0);
        score.MissingKeys.Should().NotBeEmpty();
    }

    [Fact]
    public void Golden_non_json_fixture_is_structural_parse_failure()
    {
        string json = LoadFixtureText("golden-agent-result-not-json.txt");

        AgentOutputEvaluationScore score = Structural.Evaluate(TraceId, json, AgentType.Topology);

        score.IsJsonParseFailure.Should().BeTrue();
    }

    /// <summary>
    ///     Regression: stripping per-claim evidence must reduce semantic score (Prompt 2 — golden-set guard).
    /// </summary>
    [Fact]
    public void Golden_claim_without_evidence_refs_lowers_semantic_score_relative_to_valid_fixture()
    {
        string validJson = LoadFixtureText("golden-agent-result-valid.json");
        string withoutClaimEvidenceJson = LoadFixtureText("golden-agent-result-claim-without-evidence.json");

        AgentOutputSemanticScore validSemantic = Semantic.Evaluate(TraceId, validJson, AgentType.Topology);
        AgentOutputSemanticScore strippedSemantic =
            Semantic.Evaluate(TraceId, withoutClaimEvidenceJson, AgentType.Topology);

        validSemantic.OverallSemanticScore.Should().BeApproximately(1.0, 0.001);
        strippedSemantic.ClaimsQualityRatio.Should().Be(0.0);
        strippedSemantic.EmptyClaimCount.Should().Be(1);
        strippedSemantic.OverallSemanticScore.Should().BeApproximately(0.6, 0.001);
        strippedSemantic.OverallSemanticScore.Should().BeLessThan(validSemantic.OverallSemanticScore);
    }

    private static string LoadFixtureText(string fileName)
    {
        string dir = Path.Combine(AppContext.BaseDirectory, "Fixtures", "GoldenAgentResults");
        string path = Path.Combine(dir, fileName);

        return File.ReadAllText(path);
    }
}
