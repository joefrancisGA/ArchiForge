namespace ArchiForge.Contracts.ProductLearning.Planning;

/// <summary>
/// Either an authority bundle artifact (<see cref="AuthorityBundleId"/> + <see cref="AuthorityArtifactSortOrder"/>)
/// or a pilot <see cref="PilotArtifactHint"/> — at least one form must be populated.
/// </summary>
public sealed class ProductLearningImprovementPlanArtifactLinkRecord
{
    public Guid LinkId { get; init; }
    public Guid PlanId { get; init; }
    public Guid? AuthorityBundleId { get; init; }
    public int? AuthorityArtifactSortOrder { get; init; }
    public string? PilotArtifactHint { get; init; }
}
