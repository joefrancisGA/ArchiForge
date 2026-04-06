namespace ArchiForge.Decisioning.Alerts.Simulation;

/// <summary>
/// Canonical string tokens for <see cref="RuleSimulationRequest.RuleKind"/> and related
/// threshold-recommendation paths.
/// </summary>
/// <remarks>
/// Previously duplicated as private constants in both
/// <c>ArchiForge.Persistence.Alerts.Simulation.RuleSimulationService</c> and
/// <c>ArchiForge.Decisioning.Alerts.Tuning.ThresholdRecommendationService</c>.
/// Any future caller should reference these constants to avoid silent drift.
/// </remarks>
public static class RuleKindConstants
{
    public const string Simple = "Simple";
    public const string Composite = "Composite";
}
