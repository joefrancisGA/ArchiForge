namespace ArchiForge.Contracts.Evolution;

/// <summary>Shadow run: duplicate run detail in memory, apply candidate steps on the copy, run analysis, discard the copy.</summary>
public sealed class ShadowExecutionRequest
{
    public required string BaselineArchitectureRunId { get; init; }

    public required CandidateChangeSet CandidateChangeSet { get; init; }

    public ShadowExecutionPipelineOptions? Pipeline { get; init; }
}
