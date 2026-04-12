namespace ArchLucid.Contracts.ProductLearning.Planning;

/// <summary>Associates a plan with a coordinator run id string (no-dash GUID, same convention as <c>dbo.Runs.RunId</c>).</summary>
public sealed class ProductLearningImprovementPlanRunLinkRecord
{
    public Guid PlanId { get; init; }
    public string ArchitectureRunId { get; init; } = string.Empty;
}
