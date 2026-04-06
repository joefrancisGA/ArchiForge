namespace ArchiForge.Contracts.Evolution;

/// <summary>Row in <c>EvolutionCandidateChangeSets</c>: a reviewable candidate derived from a 59R improvement plan.</summary>
public sealed class EvolutionCandidateChangeSetRecord
{
    public Guid CandidateChangeSetId { get; init; }

    public Guid TenantId { get; init; }

    public Guid WorkspaceId { get; init; }

    public Guid ProjectId { get; init; }

    public Guid SourcePlanId { get; init; }

    public string Status { get; init; } = EvolutionCandidateChangeSetStatusValues.Draft;

    public string Title { get; init; } = string.Empty;

    public string Summary { get; init; } = string.Empty;

    public string PlanSnapshotJson { get; init; } = string.Empty;

    public string DerivationRuleVersion { get; init; } = "60R-v1";

    public DateTime CreatedUtc { get; init; }

    public string? CreatedByUserId { get; init; }
}
