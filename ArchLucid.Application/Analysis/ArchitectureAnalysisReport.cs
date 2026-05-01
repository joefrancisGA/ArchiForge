using ArchLucid.Application.Determinism;
using ArchLucid.Application.Diffs;
using ArchLucid.Contracts.Agents;
using ArchLucid.Contracts.Manifest;
using ArchLucid.Contracts.Metadata;

namespace ArchLucid.Application.Analysis;

public sealed class ArchitectureAnalysisReport
{
    public ArchitectureRun Run
    {
        get;
        set;
    } = new();

    public AgentEvidencePackage? Evidence
    {
        get;
        set;
    }

    public List<AgentExecutionTrace> ExecutionTraces
    {
        get;
        set;
    } = [];

    public GoldenManifest? Manifest
    {
        get;
        set;
    }

    public string? Diagram
    {
        get;
        set;
    }

    public string? Summary
    {
        get;
        set;
    }

    public DeterminismCheckResult? Determinism
    {
        get;
        set;
    }

    public ManifestDiffResult? ManifestDiff
    {
        get;
        set;
    }

    public AgentResultDiffResult? AgentResultDiff
    {
        get;
        set;
    }

    public List<string> Warnings
    {
        get;
        set;
    } = [];
}
