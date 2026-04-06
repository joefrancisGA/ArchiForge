namespace ArchiForge.Contracts.ProductLearning.Planning;

/// <summary>
/// Links a plan to pilot feedback (<c>ProductLearningPilotSignals</c>).
/// Optional <see cref="TriageStatusSnapshot"/> preserves triage state at link time for explainability.
/// </summary>
public sealed class ProductLearningImprovementPlanSignalLinkRecord
{
    public Guid PlanId { get; init; }
    public Guid SignalId { get; init; }
    public string? TriageStatusSnapshot { get; init; }
}
