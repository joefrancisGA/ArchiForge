namespace ArchiForge.Contracts.Evolution;

/// <summary>One proposed action within a 60R candidate change set (reviewable; not executed automatically).</summary>
public sealed class CandidateChangeSetStep
{
    public int Ordinal { get; init; }

    public string ActionType { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    public string? AcceptanceCriteria { get; init; }
}
