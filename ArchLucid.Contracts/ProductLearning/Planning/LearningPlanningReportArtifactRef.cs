namespace ArchiForge.Contracts.ProductLearning.Planning;

/// <summary>Artifact link backing an improvement plan (authority bundle coordinates or pilot hint).</summary>
public sealed class LearningPlanningReportArtifactRef
{
    public Guid LinkId { get; init; }

    public Guid? AuthorityBundleId { get; init; }

    public int? AuthorityArtifactSortOrder { get; init; }

    public string? PilotArtifactHint { get; init; }
}
