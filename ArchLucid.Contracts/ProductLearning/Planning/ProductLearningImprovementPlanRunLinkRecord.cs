namespace ArchiForge.Contracts.ProductLearning.Planning;

/// <summary>Associates a plan with a legacy <c>ArchitectureRuns.RunId</c> (pilot / manifest context).</summary>
public sealed class ProductLearningImprovementPlanRunLinkRecord
{
    public Guid PlanId { get; init; }
    public string ArchitectureRunId { get; init; } = string.Empty;
}
