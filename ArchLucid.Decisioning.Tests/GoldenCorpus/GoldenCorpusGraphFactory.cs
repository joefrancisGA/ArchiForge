using System.Globalization;

using ArchLucid.Contracts.Agents;
using ArchLucid.Contracts.Common;
using ArchLucid.Contracts.Decisions;
using ArchLucid.Contracts.Manifest;
using ArchLucid.Contracts.Requests;
using ArchLucid.KnowledgeGraph.Models;

namespace ArchLucid.Decisioning.Tests.GoldenCorpus;

/// <summary>Deterministic graph bundles for golden corpus cases (simulator-free, in-process only).</summary>
public static class GoldenCorpusGraphFactory
{
    /// <summary>Returns exactly <paramref name="count"/> cases indexed <c>0..count-1</c>.</summary>
    public static IReadOnlyList<GoldenCorpusCaseDefinition> BuildCases(int count)
    {
        if (count <= 0)
            throw new ArgumentOutOfRangeException(nameof(count));

        List<GoldenCorpusCaseDefinition> list = [];

        for (int i = 0; i < count; i++)
            list.Add(BuildCase(i));

        return list;
    }

    private static GoldenCorpusCaseDefinition BuildCase(int index)
    {
        int archetype = index % 6;
        string suffix = (index / 6).ToString(CultureInfo.InvariantCulture);

        GraphSnapshot graph = archetype switch
        {
            0 => EmptyGraph(suffix),
            1 => RequirementOnlyGraph(suffix),
            2 => TopologyOnlyGraph(suffix),
            3 => CostOnlyGraph(suffix),
            4 => SecurityBaselineGraph(suffix),
            _ => FullMultiSignalGraph(suffix),
        };

        GoldenCorpusMergeInput? merge = index < 3 ? MinimalMergeBundle() : null;

        return new GoldenCorpusCaseDefinition(
            CaseFolderName: $"case-{index + 1:D2}",
            ReadmeTitle: DescribeArchetype(archetype, index),
            Graph: graph,
            Merge: merge);
    }

    private static string DescribeArchetype(int archetype, int index) => archetype switch
    {
        0 => $"Empty graph (no nodes); boundary density variant {index}.",
        1 => "Requirement node only — requirement engine surface.",
        2 => "Topology resources only — topology coverage engines.",
        3 => "Cost constraint node — cost engine.",
        4 => "Security baseline missing control — security baseline engine.",
        _ => "Multi-signal graph (requirement + topology + security + cost) — cross-category stress.",
    };

    private static GraphSnapshot EmptyGraph(string suffix) => new()
    {
        GraphSnapshotId = Guid.Parse($"00000001-0000-4000-8000-{suffix.PadLeft(12, '0')}"),
        ContextSnapshotId = Guid.Parse("10000000-0000-4000-8000-000000000001"),
        RunId = Guid.Parse("20000000-0000-4000-8000-000000000001"),
        CreatedUtc = new DateTime(2026, 1, 15, 12, 0, 0, DateTimeKind.Utc),
        Nodes = [],
        Edges = [],
        Warnings = [],
    };

    private static GraphSnapshot RequirementOnlyGraph(string suffix) => new()
    {
        GraphSnapshotId = Guid.Parse($"00000002-0000-4000-8000-{suffix.PadLeft(12, '0')}"),
        ContextSnapshotId = Guid.Parse("10000000-0000-4000-8000-000000000002"),
        RunId = Guid.Parse("20000000-0000-4000-8000-000000000002"),
        CreatedUtc = new DateTime(2026, 1, 15, 12, 0, 0, DateTimeKind.Utc),
        Nodes =
        [
            new GraphNode
            {
                NodeId = "req-1",
                NodeType = "Requirement",
                Label = $"Auth-{suffix}",
                Properties = new Dictionary<string, string> { ["text"] = "Authenticate users" },
            },
        ],
        Edges = [],
        Warnings = [],
    };

    private static GraphSnapshot TopologyOnlyGraph(string suffix) => new()
    {
        GraphSnapshotId = Guid.Parse($"00000003-0000-4000-8000-{suffix.PadLeft(12, '0')}"),
        ContextSnapshotId = Guid.Parse("10000000-0000-4000-8000-000000000003"),
        RunId = Guid.Parse("20000000-0000-4000-8000-000000000003"),
        CreatedUtc = new DateTime(2026, 1, 15, 12, 0, 0, DateTimeKind.Utc),
        Nodes =
        [
            new GraphNode
            {
                NodeId = "t1",
                NodeType = "TopologyResource",
                Label = $"vpc-{suffix}",
                Category = "network",
                Properties = [],
            },
        ],
        Edges = [],
        Warnings = [],
    };

    private static GraphSnapshot CostOnlyGraph(string suffix) => new()
    {
        GraphSnapshotId = Guid.Parse($"00000004-0000-4000-8000-{suffix.PadLeft(12, '0')}"),
        ContextSnapshotId = Guid.Parse("10000000-0000-4000-8000-000000000004"),
        RunId = Guid.Parse("20000000-0000-4000-8000-000000000004"),
        CreatedUtc = new DateTime(2026, 1, 15, 12, 0, 0, DateTimeKind.Utc),
        Nodes =
        [
            new GraphNode
            {
                NodeId = "c1",
                NodeType = "CostConstraint",
                Label = $"Budget-{suffix}",
                Properties = new Dictionary<string, string>
                {
                    ["budgetName"] = "Prod",
                    ["maxMonthlyCost"] = "2500",
                    ["costRisk"] = "medium",
                },
            },
        ],
        Edges = [],
        Warnings = [],
    };

    private static GraphSnapshot SecurityBaselineGraph(string suffix) => new()
    {
        GraphSnapshotId = Guid.Parse($"00000005-0000-4000-8000-{suffix.PadLeft(12, '0')}"),
        ContextSnapshotId = Guid.Parse("10000000-0000-4000-8000-000000000005"),
        RunId = Guid.Parse("20000000-0000-4000-8000-000000000005"),
        CreatedUtc = new DateTime(2026, 1, 15, 12, 0, 0, DateTimeKind.Utc),
        Nodes =
        [
            new GraphNode
            {
                NodeId = "s1",
                NodeType = "SecurityBaseline",
                Label = $"TLS-{suffix}",
                Properties = new Dictionary<string, string>
                {
                    ["controlId"] = "SC-8",
                    ["status"] = "missing",
                },
            },
        ],
        Edges = [],
        Warnings = [],
    };

    private static GraphSnapshot FullMultiSignalGraph(string suffix) => new()
    {
        GraphSnapshotId = Guid.Parse($"00000006-0000-4000-8000-{suffix.PadLeft(12, '0')}"),
        ContextSnapshotId = Guid.Parse("10000000-0000-4000-8000-000000000006"),
        RunId = Guid.Parse("20000000-0000-4000-8000-000000000006"),
        CreatedUtc = new DateTime(2026, 1, 15, 12, 0, 0, DateTimeKind.Utc),
        Edges =
        [
            new GraphEdge
            {
                EdgeId = "rel1",
                FromNodeId = "r1",
                ToNodeId = "t1",
                EdgeType = "RELATES_TO",
                Label = "relates to",
            },
            new GraphEdge { EdgeId = "p1", FromNodeId = "s1", ToNodeId = "t1", EdgeType = "PROTECTS", Label = "protects" },
        ],
        Nodes =
        [
            new GraphNode
            {
                NodeId = "r1",
                NodeType = "Requirement",
                Label = $"Auth-{suffix}",
                Properties = new Dictionary<string, string> { ["text"] = "Authenticate users" },
            },
            new GraphNode
            {
                NodeId = "t1",
                NodeType = "TopologyResource",
                Label = "vpc",
                Category = "network",
                Properties = [],
            },
            new GraphNode
            {
                NodeId = "s1",
                NodeType = "SecurityBaseline",
                Label = "MFA",
                Properties = new Dictionary<string, string>
                {
                    ["controlId"] = "AC-2",
                    ["status"] = "missing",
                },
            },
            new GraphNode
            {
                NodeId = "c1",
                NodeType = "CostConstraint",
                Label = "Prod",
                Properties = new Dictionary<string, string>
                {
                    ["budgetName"] = "Prod",
                    ["maxMonthlyCost"] = "5000",
                    ["costRisk"] = "high",
                },
            },
        ],
        Warnings = [],
    };

    private static GoldenCorpusMergeInput MinimalMergeBundle() => new()
    {
        MergeRunId = "golden-merge-run-1",
        ManifestVersion = "v1-golden",
        Request = new ArchitectureRequest
        {
            RequestId = "REQ-GOLDEN-001",
            SystemName = "GoldenMergeSystem",
            Description = "Golden corpus merge slice — deterministic description text.",
            Environment = "prod",
            CloudProvider = CloudProvider.Azure,
            Constraints = ["HTTPS only"],
        },
        AgentResults =
        [
            new AgentResult
            {
                ResultId = "RES-GOLD-1",
                TaskId = "TASK-GOLD-1",
                RunId = "golden-merge-run-1",
                AgentType = AgentType.Topology,
                Claims = ["Add API service"],
                EvidenceRefs = ["request"],
                Confidence = 0.9,
                ProposedChanges = new ManifestDeltaProposal
                {
                    ProposalId = "PROP-GOLD-1",
                    SourceAgent = AgentType.Topology,
                    AddedServices =
                    [
                        new ManifestService
                        {
                            ServiceId = "svc-golden-1",
                            ServiceName = "api",
                            ServiceType = ServiceType.Api,
                            RuntimePlatform = RuntimePlatform.AppService,
                        },
                    ],
                },
            },
        ],
        Evaluations = [],
        DecisionNodes = [],
        ParentManifestVersion = null,
    };
}

public sealed record GoldenCorpusCaseDefinition(
    string CaseFolderName,
    string ReadmeTitle,
    GraphSnapshot Graph,
    GoldenCorpusMergeInput? Merge);
