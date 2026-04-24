using ArchLucid.Contracts.Agents;
using ArchLucid.Contracts.Decisions;
using ArchLucid.Contracts.Requests;
using ArchLucid.KnowledgeGraph.Models;

namespace ArchLucid.Decisioning.Tests.GoldenCorpus;

/// <summary>Root JSON contract for <c>input.json</c> (graph bundle + optional merge slice).</summary>
public sealed class GoldenCorpusInputDocument
{
    public Guid RunId
    {
        get; set;
    }

    public Guid ContextSnapshotId
    {
        get; set;
    }

    public GraphSnapshot GraphSnapshot
    {
        get; set;
    } = new();

    public GoldenCorpusMergeDocument? Merge
    {
        get; set;
    }
}

/// <summary>JSON-friendly merge payload (lists deserialize cleanly).</summary>
public sealed class GoldenCorpusMergeDocument
{
    public string MergeRunId
    {
        get; set;
    } = string.Empty;

    public string ManifestVersion
    {
        get; set;
    } = string.Empty;

    public ArchitectureRequest Request
    {
        get; set;
    } = new();

    public List<AgentResult> AgentResults
    {
        get; set;
    } = [];

    public List<AgentEvaluation> Evaluations
    {
        get; set;
    } = [];

    public List<DecisionNode> DecisionNodes
    {
        get; set;
    } = [];

    public string? ParentManifestVersion
    {
        get; set;
    }

    public GoldenCorpusMergeInput ToModel() => new()
    {
        MergeRunId = MergeRunId,
        ManifestVersion = ManifestVersion,
        Request = Request,
        AgentResults = AgentResults,
        Evaluations = Evaluations,
        DecisionNodes = DecisionNodes,
        ParentManifestVersion = ParentManifestVersion,
    };
}
