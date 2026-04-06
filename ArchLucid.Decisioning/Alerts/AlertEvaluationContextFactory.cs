using ArchiForge.Core.Comparison;
using ArchiForge.Decisioning.Advisory.Learning;
using ArchiForge.Decisioning.Advisory.Models;
using ArchiForge.Decisioning.Advisory.Workflow;
using ArchiForge.Decisioning.Governance.PolicyPacks;

namespace ArchiForge.Decisioning.Alerts;

/// <summary>
/// Builds <see cref="AlertEvaluationContext"/> for orchestration paths that already have an improvement plan and merged governance (e.g. advisory scan).
/// </summary>
/// <remarks>
/// Primary caller: <c>ArchiForge.Persistence.Advisory.AdvisoryScanRunner</c> after loading <see cref="IEffectiveGovernanceLoader"/> once per scan.
/// </remarks>
public static class AlertEvaluationContextFactory
{
    /// <summary>
    /// Creates a context for advisory digest / alert evaluation with governance pre-attached so alert services skip a second loader call.
    /// </summary>
    /// <param name="tenantId">Schedule scope tenant.</param>
    /// <param name="workspaceId">Schedule scope workspace.</param>
    /// <param name="projectId">Schedule scope project.</param>
    /// <param name="runId">Latest run id used for the plan.</param>
    /// <param name="comparedToRunId">Baseline run id when comparison was used; otherwise <c>null</c>.</param>
    /// <param name="plan">Improvement plan (including <see cref="ImprovementPlan.PolicyPackAdvisoryDefaults"/> if populated).</param>
    /// <param name="comparisonResult">Comparison output when two runs were compared; otherwise <c>null</c>.</param>
    /// <param name="recommendationRecords">Recommendations stored for <paramref name="runId"/>.</param>
    /// <param name="learningProfile">Latest learning profile for the scope.</param>
    /// <param name="effectiveGovernance">Merged <see cref="PolicyPackContentDocument"/> from <see cref="IEffectiveGovernanceLoader"/>.</param>
    /// <returns>A new <see cref="AlertEvaluationContext"/> with <see cref="AlertEvaluationContext.EffectiveGovernanceContent"/> set.</returns>
    public static AlertEvaluationContext ForAdvisoryScan(
        Guid tenantId,
        Guid workspaceId,
        Guid projectId,
        Guid runId,
        Guid? comparedToRunId,
        ImprovementPlan plan,
        ComparisonResult? comparisonResult,
        IReadOnlyList<RecommendationRecord> recommendationRecords,
        RecommendationLearningProfile? learningProfile,
        PolicyPackContentDocument effectiveGovernance)
    {
        return new AlertEvaluationContext
        {
            TenantId = tenantId,
            WorkspaceId = workspaceId,
            ProjectId = projectId,
            RunId = runId,
            ComparedToRunId = comparedToRunId,
            ImprovementPlan = plan,
            ComparisonResult = comparisonResult,
            RecommendationRecords = recommendationRecords,
            LearningProfile = learningProfile,
            EffectiveGovernanceContent = effectiveGovernance,
        };
    }
}
