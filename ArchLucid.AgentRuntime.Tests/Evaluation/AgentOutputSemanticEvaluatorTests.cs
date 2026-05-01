using ArchLucid.AgentRuntime.Evaluation;
using ArchLucid.Contracts.Agents;
using ArchLucid.Contracts.Common;

using FluentAssertions;

namespace ArchLucid.AgentRuntime.Tests.Evaluation;

[Trait("Suite", "Core")]
public sealed class AgentOutputSemanticEvaluatorTests
{
    private readonly AgentOutputSemanticEvaluator _sut = new();

    [SkippableFact]
    public void Evaluate_claims_with_mixed_evidence_returns_expected_ratio()
    {
        const string json = """
                            {
                                "claims": [
                                    { "text": "A", "evidenceRefs": [{ "id": "e1" }] },
                                    { "text": "B", "evidenceRefs": [{ "id": "e2" }] },
                                    { "text": "C", "evidenceRefs": [] }
                                ],
                                "findings": []
                            }
                            """;

        AgentOutputSemanticScore score = _sut.Evaluate("t1", json, AgentType.Topology);

        score.ClaimsQualityRatio.Should().BeApproximately(2.0 / 3.0, 0.001);
        score.EmptyClaimCount.Should().Be(1);
    }

    [SkippableFact]
    public void Evaluate_findings_with_mixed_completeness_returns_expected_ratio()
    {
        const string json = """
                            {
                                "claims": [],
                                "findings": [
                                    { "severity": "High", "description": "This is a complete finding description", "recommendation": "Fix this immediately" },
                                    { "severity": "Medium", "description": "Another complete finding here", "recommendation": "Should fix soon" },
                                    { "severity": "Low", "description": "Third complete finding description", "recommendation": "Consider fixing" },
                                    { "severity": "Info", "description": "Incomplete finding", "recommendation": "Fix" }
                                ]
                            }
                            """;

        AgentOutputSemanticScore score = _sut.Evaluate("t1", json, AgentType.Cost);

        score.FindingsQualityRatio.Should().Be(0.75);
        score.IncompleteFindingCount.Should().Be(1);
    }

    [SkippableFact]
    public void Evaluate_null_json_returns_all_zeros()
    {
        AgentOutputSemanticScore score = _sut.Evaluate("t1", null, AgentType.Compliance);

        score.ClaimsQualityRatio.Should().Be(0.0);
        score.FindingsQualityRatio.Should().Be(0.0);
        score.EmptyClaimCount.Should().Be(0);
        score.IncompleteFindingCount.Should().Be(0);
        score.OverallSemanticScore.Should().Be(0.0);
    }

    [SkippableFact]
    public void Evaluate_empty_string_returns_all_zeros()
    {
        AgentOutputSemanticScore score = _sut.Evaluate("t1", "  ", AgentType.Topology);

        score.ClaimsQualityRatio.Should().Be(0.0);
        score.FindingsQualityRatio.Should().Be(0.0);
        score.OverallSemanticScore.Should().Be(0.0);
    }

    [SkippableFact]
    public void Evaluate_json_with_no_claims_or_findings_arrays_returns_all_zeros()
    {
        const string json = """{ "resultId": "r1", "confidence": 0.8 }""";

        AgentOutputSemanticScore score = _sut.Evaluate("t1", json, AgentType.Critic);

        score.ClaimsQualityRatio.Should().Be(0.0);
        score.FindingsQualityRatio.Should().Be(0.0);
        score.OverallSemanticScore.Should().Be(0.0);
    }

    [SkippableFact]
    public void Evaluate_overall_score_uses_weighted_average_when_both_present()
    {
        const string json = """
                            {
                                "claims": [
                                    { "text": "A", "evidenceRefs": [{ "id": "e1" }] },
                                    { "text": "B", "evidenceRefs": [] }
                                ],
                                "findings": [
                                    { "severity": "High", "description": "A full finding with enough text", "recommendation": "Fix this" },
                                    { "severity": "Low", "description": "Another full finding description", "recommendation": "Fix too" }
                                ]
                            }
                            """;

        AgentOutputSemanticScore score = _sut.Evaluate("t1", json, AgentType.Topology);

        double expectedClaims = 0.5;
        double expectedFindings = 1.0;
        double expectedOverall = expectedClaims * 0.4 + expectedFindings * 0.6;

        score.ClaimsQualityRatio.Should().BeApproximately(expectedClaims, 0.001);
        score.FindingsQualityRatio.Should().BeApproximately(expectedFindings, 0.001);
        score.OverallSemanticScore.Should().BeApproximately(expectedOverall, 0.001);
    }

    [SkippableFact]
    public void Evaluate_only_claims_uses_claims_ratio_as_overall()
    {
        const string json = """
                            {
                                "claims": [
                                    { "text": "A", "evidence": "some evidence text" }
                                ]
                            }
                            """;

        AgentOutputSemanticScore score = _sut.Evaluate("t1", json, AgentType.Topology);

        score.ClaimsQualityRatio.Should().Be(1.0);
        score.OverallSemanticScore.Should().Be(1.0);
    }

    [SkippableFact]
    public void Evaluate_only_findings_uses_findings_ratio_as_overall()
    {
        const string json = """
                            {
                                "findings": [
                                    { "severity": "High", "description": "A sufficiently detailed finding", "recommendation": "Fix this now" }
                                ]
                            }
                            """;

        AgentOutputSemanticScore score = _sut.Evaluate("t1", json, AgentType.Compliance);

        score.FindingsQualityRatio.Should().Be(1.0);
        score.OverallSemanticScore.Should().Be(1.0);
    }

    [SkippableFact]
    public void Evaluate_invalid_json_returns_all_zeros()
    {
        AgentOutputSemanticScore score = _sut.Evaluate("t1", "{not-json", AgentType.Topology);

        score.ClaimsQualityRatio.Should().Be(0.0);
        score.FindingsQualityRatio.Should().Be(0.0);
        score.OverallSemanticScore.Should().Be(0.0);
    }

    [SkippableFact]
    public void Evaluate_array_root_returns_all_zeros()
    {
        AgentOutputSemanticScore score = _sut.Evaluate("t1", "[1,2,3]", AgentType.Cost);

        score.OverallSemanticScore.Should().Be(0.0);
    }

    [SkippableFact]
    public void Evaluate_claim_with_evidence_string_counts_as_evidence()
    {
        const string json = """
                            {
                                "claims": [
                                    { "text": "A", "evidence": "supporting evidence" }
                                ],
                                "findings": []
                            }
                            """;

        AgentOutputSemanticScore score = _sut.Evaluate("t1", json, AgentType.Topology);

        score.ClaimsQualityRatio.Should().Be(1.0);
        score.EmptyClaimCount.Should().Be(0);
    }

    [SkippableFact]
    public void Evaluate_sets_trace_id_and_agent_type()
    {
        AgentOutputSemanticScore score = _sut.Evaluate("trace-42", null, AgentType.Critic);

        score.TraceId.Should().Be("trace-42");
        score.AgentType.Should().Be(AgentType.Critic);
    }

    [SkippableFact]
    public void Evaluate_throws_on_null_trace_id()
    {
        Action act = () => _sut.Evaluate(null!, null, AgentType.Topology);

        act.Should().Throw<ArgumentException>();
    }

    [SkippableFact]
    public void Evaluate_throws_on_empty_trace_id()
    {
        Action act = () => _sut.Evaluate("", null, AgentType.Topology);

        act.Should().Throw<ArgumentException>();
    }
}
