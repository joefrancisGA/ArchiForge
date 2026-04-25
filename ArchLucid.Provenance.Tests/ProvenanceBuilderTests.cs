using ArchLucid.ArtifactSynthesis.Models;
using ArchLucid.Contracts.DecisionTraces;
using ArchLucid.Decisioning.Models;
using ArchLucid.KnowledgeGraph.Models;

using FluentAssertions;

namespace ArchLucid.Provenance.Tests;

/// <summary>
///     Covers <see cref="ProvenanceBuilder" /> graph construction (high line count; drives package coverage gate).
/// </summary>
[Trait("Suite", "Core")]
[Trait("Category", "Unit")]
public sealed class ProvenanceBuilderTests
{
    private static readonly Guid RunId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid ManifestId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly Guid ArtifactId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");

    [Fact]
    public void Build_minimal_inputs_yields_only_manifest_node_and_no_edges()
    {
        ProvenanceBuilder sut = new();
        DecisionProvenanceGraph graph = sut.Build(
            RunId,
            new FindingsSnapshot { Findings = [] },
            new GraphSnapshot { Nodes = [] },
            new GoldenManifest { ManifestId = ManifestId, ManifestHash = "hash-min", Decisions = [] },
            RuleAuditTrace.From(
                new RuleAuditTracePayload { AppliedRuleIds = [] }),
            []);

        graph.RunId.Should().Be(RunId);
        graph.Nodes.Should().ContainSingle(n => n.Type == ProvenanceNodeType.Manifest);
        graph.Edges.Should().BeEmpty();
    }

    [Fact]
    public void Build_full_chain_materializes_nodes_and_all_edge_types()
    {
        const string graphNodeId = "gn-1";
        const string findingId = "find-1";
        const string decisionId = "dec-1";
        const string ruleId = "rule-z";

        GraphSnapshot graphSnap = new()
        {
            Nodes =
            [
                new GraphNode { NodeId = graphNodeId, NodeType = "Service", Label = "Labeled node", Category = "cat" },
                new GraphNode
                {
                    NodeId = graphNodeId, NodeType = "Service", Label = "duplicate id same key", Category = "cat"
                },
                new GraphNode { NodeId = "bare", NodeType = "Resource", Label = "   ", Category = null }
            ]
        };

        FindingsSnapshot findings = new()
        {
            Findings =
            [
                new Finding
                {
                    FindingId = findingId,
                    FindingType = "Compliance",
                    Category = "sec",
                    EngineType = "e",
                    Severity = FindingSeverity.Warning,
                    Title = "Finding title",
                    Rationale = "r",
                    RelatedNodeIds = [graphNodeId, "not-in-graph"]
                }
            ]
        };

        ResolvedArchitectureDecision decision = new()
        {
            DecisionId = decisionId,
            Category = "c",
            Title = "Decide",
            SelectedOption = "opt",
            Rationale = "why",
            SupportingFindingIds = [findingId]
        };

        GoldenManifest manifest = new() { ManifestId = ManifestId, ManifestHash = "mh", Decisions = [decision] };

        RuleAuditTracePayload audit = new() { AppliedRuleIds = [ruleId, ruleId, ruleId.ToUpperInvariant()] };

        SynthesizedArtifact artifact = new()
        {
            ArtifactId = ArtifactId,
            ArtifactType = "doc",
            Name = "overview.md",
            Format = "md",
            Content = "x",
            ContentHash = "h",
            ContributingDecisionIds = [decisionId, "missing-decision"]
        };

        ProvenanceBuilder sut = new();
        DecisionProvenanceGraph graph = sut.Build(
            RunId,
            findings,
            graphSnap,
            manifest,
            RuleAuditTrace.From(audit),
            [artifact]);

        graph.Nodes.Should().HaveCount(7);
        graph.Nodes.Should().Contain(n => n.Type == ProvenanceNodeType.GraphNode && n.ReferenceId == graphNodeId);
        graph.Nodes.Should().Contain(n =>
            n.Type == ProvenanceNodeType.GraphNode && n.ReferenceId == "bare" && n.Name == "bare");
        graph.Nodes.Should().Contain(n => n.Type == ProvenanceNodeType.Finding && n.ReferenceId == findingId);
        graph.Nodes.Should().Contain(n => n.Type == ProvenanceNodeType.Rule && n.ReferenceId == ruleId);
        graph.Nodes.Should().Contain(n => n.Type == ProvenanceNodeType.Decision && n.ReferenceId == decisionId);
        graph.Nodes.Should().Contain(n => n.Type == ProvenanceNodeType.Artifact);
        graph.Nodes.Should().Contain(n => n.Type == ProvenanceNodeType.Manifest);

        graph.Edges.Should().NotBeEmpty();
        graph.Edges.Should().Contain(e => e.Type == ProvenanceEdgeType.SupportedBy);
        graph.Edges.Should().Contain(e => e.Type == ProvenanceEdgeType.InfluencedByGraphNode);
        graph.Edges.Should().Contain(e => e.Type == ProvenanceEdgeType.TriggeredByRule);
        graph.Edges.Should().Contain(e => e.Type == ProvenanceEdgeType.ContributedToArtifact);
        graph.Edges.Should().Contain(e => e.Type == ProvenanceEdgeType.ContainedInManifest);
    }

    [Fact]
    public void Build_skips_supported_by_when_finding_node_missing()
    {
        ResolvedArchitectureDecision decision = new()
        {
            DecisionId = "d-alone",
            Category = "c",
            Title = "T",
            SelectedOption = "o",
            Rationale = "r",
            SupportingFindingIds = ["no-such-finding"]
        };

        ProvenanceBuilder sut = new();
        DecisionProvenanceGraph graph = sut.Build(
            RunId,
            new FindingsSnapshot { Findings = [] },
            new GraphSnapshot { Nodes = [] },
            new GoldenManifest { ManifestId = ManifestId, ManifestHash = "h", Decisions = [decision] },
            RuleAuditTrace.From(new RuleAuditTracePayload { AppliedRuleIds = [] }),
            []);

        graph.Edges.Should().ContainSingle(e => e.Type == ProvenanceEdgeType.ContainedInManifest);
        graph.Edges.Should().NotContain(e => e.Type == ProvenanceEdgeType.SupportedBy);
    }

    [Fact]
    public void Build_duplicate_finding_id_reuses_single_node()
    {
        Finding f = new()
        {
            FindingId = "same",
            FindingType = "t",
            Category = "c",
            EngineType = "e",
            Severity = FindingSeverity.Info,
            Title = "a",
            Rationale = "r"
        };

        ProvenanceBuilder sut = new();
        DecisionProvenanceGraph graph = sut.Build(
            RunId,
            new FindingsSnapshot { Findings = [f, f] },
            new GraphSnapshot { Nodes = [] },
            new GoldenManifest { ManifestId = ManifestId, ManifestHash = "h", Decisions = [] },
            RuleAuditTrace.From(new RuleAuditTracePayload { AppliedRuleIds = [] }),
            []);

        graph.Nodes.Count(n => n.Type == ProvenanceNodeType.Finding).Should().Be(1);
    }
}
