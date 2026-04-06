namespace ArchiForge.Api.Models.Evolution;

/// <summary>Change set description block in a simulation export document.</summary>
public sealed class EvolutionSimulationReportCandidateSection
{
    public Guid CandidateChangeSetId { get; init; }

    public Guid SourcePlanId { get; init; }

    public required string Status { get; init; }

    public required string Title { get; init; }

    public required string Summary { get; init; }

    public required string DerivationRuleVersion { get; init; }

    public DateTime CreatedUtc { get; init; }

    public string? CreatedByUserId { get; init; }
}
