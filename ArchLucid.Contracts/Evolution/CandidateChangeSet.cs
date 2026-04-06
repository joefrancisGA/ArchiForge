namespace ArchiForge.Contracts.Evolution;

/// <summary>60R candidate change set: a reviewable package derived from a 59R improvement plan (simulation-only until explicitly approved).</summary>
public sealed class CandidateChangeSet
{
    public Guid ChangeSetId { get; init; }

    public Guid SourcePlanId { get; init; }

    public string Description { get; init; } = string.Empty;

    public IReadOnlyList<CandidateChangeSetStep> ProposedActions { get; init; } = [];

    public IReadOnlyList<ChangeSetAffectedComponent> AffectedComponents { get; init; } = [];

    public ExpectedImpact ExpectedImpact { get; init; } = new();

    public double? SimulationScore { get; init; }

    public double? DeterminismScore { get; init; }

    public double? RegressionRiskScore { get; init; }

    public ApprovalStatus ApprovalStatus { get; init; }

    public DateTime CreatedUtc { get; init; }
}
