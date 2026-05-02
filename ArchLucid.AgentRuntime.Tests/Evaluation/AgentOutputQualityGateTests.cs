using ArchLucid.AgentRuntime.Evaluation;
using ArchLucid.Contracts.Agents;
using ArchLucid.Contracts.Common;
using ArchLucid.Core.Configuration;

using FluentAssertions;

using Microsoft.Extensions.Options;

namespace ArchLucid.AgentRuntime.Tests.Evaluation;

[Trait("Suite", "Core")]
[Trait("Category", "Unit")]
public sealed class AgentOutputQualityGateTests
{
    private static AgentOutputEvaluationScore Structural(double ratio)
    {
        return new AgentOutputEvaluationScore
        {
            TraceId = "t",
            AgentType = AgentType.Topology,
            StructuralCompletenessRatio = ratio,
            IsJsonParseFailure = false
        };
    }

    private static AgentOutputSemanticScore Semantic(double overall)
    {
        return new AgentOutputSemanticScore
        {
            TraceId = "t", AgentType = AgentType.Topology, OverallSemanticScore = overall
        };
    }

    [SkippableFact]
    public void Evaluate_when_disabled_always_accepts()
    {
        AgentOutputQualityGate sut = new(Options.Create(new AgentOutputQualityGateOptions { Enabled = false }));

        AgentOutputQualityGateOutcome o = sut.Evaluate(Structural(0.1), Semantic(0.1));

        o.Should().Be(AgentOutputQualityGateOutcome.Accepted);
    }

    [SkippableFact]
    public void Evaluate_rejects_when_structural_below_reject_floor()
    {
        AgentOutputQualityGate sut = new(Options.Create(new AgentOutputQualityGateOptions
        {
            Enabled = true,
            StructuralRejectBelow = 0.35,
            SemanticRejectBelow = 0.35,
            StructuralWarnBelow = 0.55,
            SemanticWarnBelow = 0.55
        }));

        sut.Evaluate(Structural(0.34), Semantic(0.9)).Should().Be(AgentOutputQualityGateOutcome.Rejected);
    }

    [SkippableFact]
    public void Evaluate_warns_when_scores_below_warn_but_above_reject_with_explicit_floors()
    {
        AgentOutputQualityGate sut = new(Options.Create(new AgentOutputQualityGateOptions
        {
            Enabled = true,
            StructuralRejectBelow = 0.35,
            SemanticRejectBelow = 0.35,
            StructuralWarnBelow = 0.55,
            SemanticWarnBelow = 0.55
        }));

        sut.Evaluate(Structural(0.5), Semantic(0.5)).Should().Be(AgentOutputQualityGateOutcome.Warned);
    }

    [SkippableFact]
    public void Evaluate_accepts_when_at_or_above_explicit_warn_thresholds()
    {
        AgentOutputQualityGate sut = new(Options.Create(new AgentOutputQualityGateOptions
        {
            Enabled = true,
            StructuralRejectBelow = 0.35,
            SemanticRejectBelow = 0.35,
            StructuralWarnBelow = 0.55,
            SemanticWarnBelow = 0.55
        }));

        sut.Evaluate(Structural(0.56), Semantic(0.56)).Should().Be(AgentOutputQualityGateOutcome.Accepted);
    }

    [SkippableFact]
    public void Evaluate_shipped_defaults_warn_on_critical_semantic_gap()
    {
        AgentOutputQualityGate sut = new(Options.Create(new AgentOutputQualityGateOptions()));

        sut.Evaluate(Structural(1.0), Semantic(0.0)).Should().Be(AgentOutputQualityGateOutcome.Warned);
    }

    [SkippableFact]
    public void Evaluate_shipped_defaults_accept_at_or_above_warn_floors()
    {
        AgentOutputQualityGate sut = new(Options.Create(new AgentOutputQualityGateOptions()));

        sut.Evaluate(Structural(0.31), Semantic(0.21)).Should().Be(AgentOutputQualityGateOutcome.Accepted);
    }

    [SkippableFact]
    public void Evaluate_shipped_defaults_does_not_reject_when_reject_floors_are_zero()
    {
        AgentOutputQualityGate sut = new(Options.Create(new AgentOutputQualityGateOptions()));

        sut.Evaluate(Structural(0.01), Semantic(0.01)).Should().Be(AgentOutputQualityGateOutcome.Warned);
    }

    [SkippableFact]
    public void EnforceOnReject_defaults_to_false()
    {
        AgentOutputQualityGateOptions options = new();

        options.EnforceOnReject.Should().BeFalse("default must be false so existing behaviour is preserved");
    }
}
