using ArchLucid.Application.Analysis;
using ArchLucid.Application.Determinism;
using ArchLucid.Contracts.Evolution;

namespace ArchLucid.Application.Evolution;

/// <summary>Inputs for scoring: baseline/simulated analysis reports and optional determinism payload.</summary>
public sealed class SimulationEvaluationRequest
{
    public required ArchitectureAnalysisReport BaselineReport
    {
        get;
        init;
    }

    public ArchitectureAnalysisReport? SimulatedReport
    {
        get;
        init;
    }

    /// <summary>When set, takes precedence over <see cref="ArchitectureAnalysisReport.Determinism" /> on the baseline report.</summary>
    public DeterminismCheckResult? SuppliedDeterminism
    {
        get;
        init;
    }

    /// <summary>Optional run id for live determinism when options do not specify one.</summary>
    public string? BaselineArchitectureRunId
    {
        get;
        init;
    }

    public SimulationEvaluationOptions? Options
    {
        get;
        init;
    }
}
