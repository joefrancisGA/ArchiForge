using ArchLucid.Provenance.Analysis;

using FluentAssertions;

namespace ArchLucid.Provenance.Tests.Analysis;

[Trait("Suite", "Core")]
[Trait("Category", "Unit")]
public sealed class ProvenanceCompletenessAnalyzerTests
{
    private static readonly Guid GraphNodeId = Guid.Parse("10000000-0000-0000-0000-000000000001");
    private static readonly Guid FindingId = Guid.Parse("20000000-0000-0000-0000-000000000002");
    private static readonly Guid RuleId = Guid.Parse("30000000-0000-0000-0000-000000000003");
    private static readonly Guid DecisionId = Guid.Parse("40000000-0000-0000-0000-000000000004");
    private static readonly Guid Decision2Id = Guid.Parse("50000000-0000-0000-0000-000000000005");

    [Fact]
    public void Analyze_Throws_WhenGraphIsNull()
    {
        Action act = () => ProvenanceCompletenessAnalyzer.Analyze(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Analyze_EmptyGraph_HasFullRatio_AndNoUncoveredKeys()
    {
        DecisionProvenanceGraph graph = new() { Nodes = [], Edges = [] };

        ProvenanceCompletenessResult result = ProvenanceCompletenessAnalyzer.Analyze(graph);

        result.TotalDecisions.Should().Be(0);
        result.DecisionsCovered.Should().Be(0);
        result.CoverageRatio.Should().Be(1.0);
        result.UncoveredDecisionKeys.Should().BeEmpty();
    }

    [Fact]
    public void Analyze_TreatsNullNodesAsEmpty()
    {
        DecisionProvenanceGraph graph = new() { Nodes = null!, Edges = [] };

        ProvenanceCompletenessResult result = ProvenanceCompletenessAnalyzer.Analyze(graph);

        result.CoverageRatio.Should().Be(1.0);
        result.TotalDecisions.Should().Be(0);
    }

    [Fact]
    public void Analyze_TreatsNullEdgesAsEmpty_UncoveredDecision()
    {
        DecisionProvenanceGraph graph = new()
        {
            Nodes =
            [
                new ProvenanceNode
                {
                    Id = DecisionId, Type = ProvenanceNodeType.Decision, ReferenceId = "dec-only", Name = "Lonely"
                }
            ],
            Edges = null!
        };

        ProvenanceCompletenessResult result = ProvenanceCompletenessAnalyzer.Analyze(graph);

        result.TotalDecisions.Should().Be(1);
        result.DecisionsCovered.Should().Be(0);
        result.CoverageRatio.Should().Be(0.0);
        result.UncoveredDecisionKeys.Should().Equal("dec-only");
    }

    [Fact]
    public void Analyze_FullyLinkedDecision_IsCovered()
    {
        DecisionProvenanceGraph graph = BuildMinimalCoveredGraph();

        ProvenanceCompletenessResult result = ProvenanceCompletenessAnalyzer.Analyze(graph);

        result.TotalDecisions.Should().Be(1);
        result.DecisionsCovered.Should().Be(1);
        result.CoverageRatio.Should().Be(1.0);
        result.UncoveredDecisionKeys.Should().BeEmpty();
    }

    [Fact]
    public void Analyze_DecisionWithoutSupportedBy_IsUncovered()
    {
        DecisionProvenanceGraph graph = BuildMinimalCoveredGraph();
        graph.Edges.RemoveAll(e => e.Type == ProvenanceEdgeType.SupportedBy);

        ProvenanceCompletenessResult result = ProvenanceCompletenessAnalyzer.Analyze(graph);

        result.DecisionsCovered.Should().Be(0);
        result.CoverageRatio.Should().Be(0.0);
        result.UncoveredDecisionKeys.Should().Equal("dec-a");
    }

    [Fact]
    public void Analyze_DecisionWithoutTriggeredByRule_IsUncovered()
    {
        DecisionProvenanceGraph graph = BuildMinimalCoveredGraph();
        graph.Edges.RemoveAll(e => e.Type == ProvenanceEdgeType.TriggeredByRule);

        ProvenanceCompletenessResult result = ProvenanceCompletenessAnalyzer.Analyze(graph);

        result.DecisionsCovered.Should().Be(0);
        result.CoverageRatio.Should().Be(0.0);
        result.UncoveredDecisionKeys.Should().ContainSingle().Which.Should().Be("dec-a");
    }

    [Fact]
    public void Analyze_DecisionWithoutGraphInfluenceOnSupportingFinding_IsUncovered()
    {
        DecisionProvenanceGraph graph = BuildMinimalCoveredGraph();
        graph.Edges.RemoveAll(e => e.Type == ProvenanceEdgeType.InfluencedByGraphNode);

        ProvenanceCompletenessResult result = ProvenanceCompletenessAnalyzer.Analyze(graph);

        result.DecisionsCovered.Should().Be(0);
        result.CoverageRatio.Should().Be(0.0);
        result.UncoveredDecisionKeys.Should().Equal("dec-a");
    }

    [Fact]
    public void Analyze_SupportedByFromNonFinding_DoesNotSatisfyFindingOrGraphRequirement()
    {
        DecisionProvenanceGraph graph = BuildMinimalCoveredGraph();
        ProvenanceEdge bad = graph.Edges.Single(e => e.Type == ProvenanceEdgeType.SupportedBy);
        bad.FromNodeId = RuleId;

        ProvenanceCompletenessResult result = ProvenanceCompletenessAnalyzer.Analyze(graph);

        result.DecisionsCovered.Should().Be(0);
        result.UncoveredDecisionKeys.Should().Equal("dec-a");
    }

    [Fact]
    public void Analyze_TwoDecisions_OnePartiallyLinked_HalvesRatio()
    {
        DecisionProvenanceGraph graph = BuildMinimalCoveredGraph();
        ProvenanceNode secondDecision = new()
        {
            Id = Decision2Id, Type = ProvenanceNodeType.Decision, ReferenceId = "dec-b", Name = "Second"
        };
        graph.Nodes.Add(secondDecision);
        graph.Edges.Add(
            new ProvenanceEdge
            {
                Id = Guid.NewGuid(),
                FromNodeId = FindingId,
                ToNodeId = Decision2Id,
                Type = ProvenanceEdgeType.SupportedBy
            });

        ProvenanceCompletenessResult result = ProvenanceCompletenessAnalyzer.Analyze(graph);

        result.TotalDecisions.Should().Be(2);
        result.DecisionsCovered.Should().Be(1);
        result.CoverageRatio.Should().Be(0.5);
        result.UncoveredDecisionKeys.Should().Equal("dec-b");
    }

    [Fact]
    public void Analyze_WhenOneSupportingFindingLacksGraphButAnotherHas_StillCovered()
    {
        Guid finding2Id = Guid.Parse("60000000-0000-0000-0000-000000000006");
        DecisionProvenanceGraph graph = BuildMinimalCoveredGraph();
        graph.Nodes.Add(
            new ProvenanceNode
            {
                Id = finding2Id, Type = ProvenanceNodeType.Finding, ReferenceId = "finding-b", Name = "B"
            });
        graph.Edges.Add(
            new ProvenanceEdge
            {
                Id = Guid.NewGuid(),
                FromNodeId = finding2Id,
                ToNodeId = DecisionId,
                Type = ProvenanceEdgeType.SupportedBy
            });

        ProvenanceCompletenessResult result = ProvenanceCompletenessAnalyzer.Analyze(graph);

        result.DecisionsCovered.Should().Be(1);
        result.CoverageRatio.Should().Be(1.0);
    }

    private static DecisionProvenanceGraph BuildMinimalCoveredGraph()
    {
        List<ProvenanceNode> nodes =
        [
            new() { Id = GraphNodeId, Type = ProvenanceNodeType.GraphNode, ReferenceId = "gn-1", Name = "Graph" },
            new() { Id = FindingId, Type = ProvenanceNodeType.Finding, ReferenceId = "finding-a", Name = "Finding" },
            new() { Id = RuleId, Type = ProvenanceNodeType.Rule, ReferenceId = "rule-1", Name = "rule-1" },
            new() { Id = DecisionId, Type = ProvenanceNodeType.Decision, ReferenceId = "dec-a", Name = "Decision" }
        ];

        List<ProvenanceEdge> edges =
        [
            new()
            {
                Id = Guid.NewGuid(),
                FromNodeId = GraphNodeId,
                ToNodeId = FindingId,
                Type = ProvenanceEdgeType.InfluencedByGraphNode
            },
            new()
            {
                Id = Guid.NewGuid(),
                FromNodeId = FindingId,
                ToNodeId = DecisionId,
                Type = ProvenanceEdgeType.SupportedBy
            },
            new()
            {
                Id = Guid.NewGuid(),
                FromNodeId = RuleId,
                ToNodeId = DecisionId,
                Type = ProvenanceEdgeType.TriggeredByRule
            }
        ];

        return new DecisionProvenanceGraph { Nodes = nodes, Edges = edges };
    }
}
