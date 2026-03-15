namespace ArchiForge.Application.Determinism;

public sealed class DeterminismCheckResult
{
    public string SourceRunId { get; set; } = string.Empty;

    public int Iterations { get; set; }

    public string ExecutionMode { get; set; } = string.Empty;

    public bool IsDeterministic { get; set; }

    public string BaselineReplayRunId { get; set; } = string.Empty;

    public List<DeterminismIterationResult> IterationResults { get; set; } = [];

    public List<string> Warnings { get; set; } = [];
}
