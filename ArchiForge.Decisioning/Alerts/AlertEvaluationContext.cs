using ArchiForge.Core.Comparison;
using ArchiForge.Decisioning.Advisory.Learning;
using ArchiForge.Decisioning.Advisory.Models;
using ArchiForge.Decisioning.Advisory.Workflow;
using ArchiForge.Decisioning.Governance.PolicyPacks;

namespace ArchiForge.Decisioning.Alerts;

public class AlertEvaluationContext
{
    public Guid TenantId { get; set; }
    public Guid WorkspaceId { get; set; }
    public Guid ProjectId { get; set; }

    public Guid? RunId { get; set; }
    public Guid? ComparedToRunId { get; set; }

    public ImprovementPlan? ImprovementPlan { get; set; }
    public ComparisonResult? ComparisonResult { get; set; }

    public IReadOnlyList<RecommendationRecord> RecommendationRecords { get; set; } = [];
    public RecommendationLearningProfile? LearningProfile { get; set; }

    /// <summary>
    /// When set (e.g. advisory scan), alert services reuse this merged document instead of calling
    /// <see cref="IEffectiveGovernanceLoader.LoadEffectiveContentAsync"/> again.
    /// </summary>
    public PolicyPackContentDocument? EffectiveGovernanceContent { get; set; }
}
