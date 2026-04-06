namespace ArchiForge.Contracts.Evolution;

/// <summary>Structured view of a component or workflow area touched by a candidate change set (from 59R theme/plan context).</summary>
public sealed class ChangeSetAffectedComponent
{
    public string ComponentKey { get; init; } = string.Empty;

    public string DisplayName { get; init; } = string.Empty;

    public string WorkflowArea { get; init; } = string.Empty;
}
