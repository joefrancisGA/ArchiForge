using ArchiForge.Decisioning.Models;
using ArchiForge.Decisioning.Services;
using ArchiForge.KnowledgeGraph.Models;

using FluentAssertions;

namespace ArchiForge.Decisioning.Tests;

public sealed class PolicyApplicabilityFindingEngineTests
{
    [Fact]
    public async Task Emits_info_when_APPLIES_TO_topology_targets_exist()
    {
        PolicyApplicabilityFindingEngine engine = new();
        GraphSnapshot graph = new()
        {
            Nodes =
            [
                new GraphNode { NodeId = "p1", NodeType = "PolicyControl", Label = "Encryption", Properties = new() },
                new GraphNode { NodeId = "t1", NodeType = "TopologyResource", Label = "vnet", Category = "network", Properties = new() }
            ],
            Edges =
            [
                new GraphEdge
                {
                    EdgeId = "e1",
                    FromNodeId = "p1",
                    ToNodeId = "t1",
                    EdgeType = "APPLIES_TO",
                    Label = "applies to"
                }
            ]
        };

        IReadOnlyList<Finding> findings = await engine.AnalyzeAsync(graph, CancellationToken.None);

        FindingPayloadValidator payloadValidator = new();
        foreach (Finding finding in findings)
            payloadValidator.Validate(finding);

        findings.Should().ContainSingle(x =>
            x.FindingType == "PolicyApplicabilityFinding" && x.Severity == FindingSeverity.Info);
        Finding infoFinding = findings.Single(x => x.Severity == FindingSeverity.Info);
        infoFinding.RelatedNodeIds.Should().Contain("p1");
        infoFinding.RelatedNodeIds.Should().Contain("t1");
    }

    [Fact]
    public async Task Emits_warning_when_topology_exists_but_policy_has_no_APPLIES_TO()
    {
        PolicyApplicabilityFindingEngine engine = new();
        GraphSnapshot graph = new()
        {
            Nodes =
            [
                new GraphNode { NodeId = "p1", NodeType = "PolicyControl", Label = "Retention", Properties = new() },
                new GraphNode { NodeId = "t1", NodeType = "TopologyResource", Label = "storage", Category = "storage", Properties = new() }
            ],
            Edges = []
        };

        IReadOnlyList<Finding> findings = await engine.AnalyzeAsync(graph, CancellationToken.None);

        FindingPayloadValidator payloadValidator = new();
        foreach (Finding finding in findings)
            payloadValidator.Validate(finding);

        findings.Should().ContainSingle(x =>
            x.FindingType == "PolicyApplicabilityFinding" && x.Severity == FindingSeverity.Warning);
    }
}
