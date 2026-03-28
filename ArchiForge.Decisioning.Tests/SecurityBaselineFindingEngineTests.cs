using ArchiForge.Decisioning.Findings.Payloads;
using ArchiForge.Decisioning.Models;
using ArchiForge.Decisioning.Services;
using ArchiForge.KnowledgeGraph.Models;

using FluentAssertions;

namespace ArchiForge.Decisioning.Tests;

/// <summary>
/// Unit tests for <see cref="SecurityBaselineFindingEngine"/>:
/// empty graph, single node without PROTECTS, missing status becomes Error,
/// present status becomes Info, PROTECTS edges expand relatedNodeIds.
/// </summary>
[Trait("Suite", "Core")]
[Trait("Category", "Unit")]
public sealed class SecurityBaselineFindingEngineTests
{
    private readonly SecurityBaselineFindingEngine _sut = new();

    // ──────────────────────────────────────────────────────────────────────────
    // Helpers
    // ──────────────────────────────────────────────────────────────────────────

    private static GraphSnapshot EmptySnapshot() => new()
    {
        GraphSnapshotId = Guid.NewGuid(),
        ContextSnapshotId = Guid.NewGuid(),
        Nodes = [],
        Edges = []
    };

    private static GraphNode SecurityNode(string nodeId, string label, string? controlId = null, string? status = null)
    {
        GraphNode node = new()
        {
            NodeId = nodeId,
            Label = label,
            NodeType = "SecurityBaseline",
            Properties = []
        };

        if (controlId is not null)
            node.Properties["controlId"] = controlId;

        if (status is not null)
            node.Properties["status"] = status;

        return node;
    }

    private static GraphNode ResourceNode(string nodeId, string label) => new()
    {
        NodeId = nodeId,
        Label = label,
        NodeType = "Resource",
        Properties = []
    };

    private static GraphSnapshot SnapshotWith(List<GraphNode> nodes, List<GraphEdge> edges) => new()
    {
        GraphSnapshotId = Guid.NewGuid(),
        ContextSnapshotId = Guid.NewGuid(),
        Nodes = nodes,
        Edges = edges
    };

    // ──────────────────────────────────────────────────────────────────────────
    // Scenario 1: empty graph → no findings
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task AnalyzeAsync_EmptyGraph_ReturnsNoFindings()
    {
        IReadOnlyList<Finding> findings = await _sut.AnalyzeAsync(EmptySnapshot(), CancellationToken.None);

        findings.Should().BeEmpty();
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Scenario 2: single SecurityBaseline node, no PROTECTS edges
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task AnalyzeAsync_SingleNodeNoEdges_OneFindingWithNodeOnly()
    {
        GraphNode node = SecurityNode("sec-1", "MFA Enforcement", controlId: "ctrl-01", status: "present");
        GraphSnapshot snapshot = SnapshotWith([node], []);

        IReadOnlyList<Finding> findings = await _sut.AnalyzeAsync(snapshot, CancellationToken.None);

        findings.Should().HaveCount(1);
        Finding f = findings[0];
        f.FindingType.Should().Be("SecurityControlFinding");
        f.Severity.Should().Be(FindingSeverity.Info);
        f.RelatedNodeIds.Should().ContainSingle(id => id == "sec-1");
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Scenario 3: status "missing" → Error severity
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task AnalyzeAsync_StatusMissing_SeverityIsError()
    {
        GraphNode node = SecurityNode("sec-2", "Encryption at Rest", controlId: "ctrl-02", status: "missing");
        GraphSnapshot snapshot = SnapshotWith([node], []);

        IReadOnlyList<Finding> findings = await _sut.AnalyzeAsync(snapshot, CancellationToken.None);

        findings.Should().HaveCount(1);
        Finding f = findings[0];
        f.Severity.Should().Be(FindingSeverity.Error);

        SecurityControlFindingPayload? payload = f.Payload as SecurityControlFindingPayload;
        payload.Should().NotBeNull();
        payload.Impact.Should().Contain("missing");
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Scenario 4: status "present" (case-insensitive) → Info severity
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task AnalyzeAsync_StatusPresent_SeverityIsInfo()
    {
        GraphNode node = SecurityNode("sec-3", "RBAC", controlId: "ctrl-03", status: "Present");
        GraphSnapshot snapshot = SnapshotWith([node], []);

        IReadOnlyList<Finding> findings = await _sut.AnalyzeAsync(snapshot, CancellationToken.None);

        findings[0].Severity.Should().Be(FindingSeverity.Info);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Scenario 5: PROTECTS edge → target node id included in RelatedNodeIds
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task AnalyzeAsync_WithProtectsEdge_TargetIncludedInRelatedNodeIds()
    {
        GraphNode secNode = SecurityNode("sec-4", "Network Segmentation", controlId: "ctrl-04", status: "present");
        GraphNode resNode = ResourceNode("res-1", "VNet");

        GraphEdge edge = new()
        {
            EdgeId = Guid.NewGuid().ToString("D"),
            FromNodeId = "sec-4",
            ToNodeId = "res-1",
            EdgeType = "PROTECTS"
        };

        GraphSnapshot snapshot = SnapshotWith([secNode, resNode], [edge]);

        IReadOnlyList<Finding> findings = await _sut.AnalyzeAsync(snapshot, CancellationToken.None);

        Finding f = findings[0];
        f.RelatedNodeIds.Should().Contain("sec-4");
        f.RelatedNodeIds.Should().Contain("res-1");
        f.RelatedNodeIds.Should().HaveCount(2);
        f.Rationale.Should().Contain("PROTECTS");
    }
}
