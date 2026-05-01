using ArchLucid.Contracts.Evolution;

namespace ArchLucid.Application.Evolution;

/// <summary>Deterministic evaluation output with explainable JSON detail.</summary>
public sealed class SimulationEvaluationResult
{
    public required EvaluationScore Score
    {
        get;
        init;
    }

    public required string ExplanationSummary
    {
        get;
        init;
    }

    public string? ExplanationDetailJson
    {
        get;
        init;
    }
}
