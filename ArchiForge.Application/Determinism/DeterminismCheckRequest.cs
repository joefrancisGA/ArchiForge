namespace ArchiForge.Application.Determinism;

public sealed class DeterminismCheckRequest
{
    public string RunId { get; set; } = string.Empty;

    public int Iterations { get; set; } = 5;

    public string ExecutionMode { get; set; } = "Current";

    public bool CommitReplays { get; set; } = false;
}
