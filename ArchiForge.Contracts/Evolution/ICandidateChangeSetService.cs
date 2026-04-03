using ArchiForge.Contracts.ProductLearning.Planning;

namespace ArchiForge.Contracts.Evolution;

/// <summary>
/// Maps persisted 59R improvement plans into deterministic 60R <see cref="CandidateChangeSet"/> projections (no execution or persistence).
/// </summary>
public interface ICandidateChangeSetService
{
    /// <summary>
    /// Produces one aggregate change set (all steps), plus one change set per action step when the plan has multiple steps.
    /// Ordering and identifiers are stable for the same inputs.
    /// </summary>
    /// <param name="plan">Required improvement plan record.</param>
    /// <param name="theme">Optional parent theme for affected-component and impact context.</param>
    IReadOnlyList<CandidateChangeSet> MapFromImprovementPlan(
        ProductLearningImprovementPlanRecord plan,
        ProductLearningImprovementThemeRecord? theme);
}
