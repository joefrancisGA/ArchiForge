using ArchiForge.Core.Comparison;
using ArchiForge.Decisioning.Advisory.Learning;
using ArchiForge.Decisioning.Advisory.Models;
using ArchiForge.Decisioning.Advisory.Workflow;

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

    public IReadOnlyList<RecommendationRecord> RecommendationRecords { get; set; } = Array.Empty<RecommendationRecord>();
    public RecommendationLearningProfile? LearningProfile { get; set; }
}
