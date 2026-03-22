using ArchiForge.Decisioning.Advisory.Learning;
using ArchiForge.Core.Comparison;
using ArchiForge.Decisioning.Advisory.Models;
using ArchiForge.Decisioning.Advisory.Workflow;
using ArchiForge.Decisioning.Governance.PolicyPacks;

namespace ArchiForge.Decisioning.Alerts;

/// <summary>Builds <see cref="AlertEvaluationContext"/> for orchestration paths (advisory scan, etc.).</summary>
public static class AlertEvaluationContextFactory
{
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
