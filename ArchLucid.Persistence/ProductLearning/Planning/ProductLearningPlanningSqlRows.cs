using System.Diagnostics.CodeAnalysis;

namespace ArchiForge.Persistence.ProductLearning.Planning;

[ExcludeFromCodeCoverage(Justification = "Dapper row-mapping DTO with no logic.")]
internal sealed class ProductLearningScopeSqlRow
{
    public Guid TenantId { get; init; }
    public Guid WorkspaceId { get; init; }
    public Guid ProjectId { get; init; }
}

internal sealed class ProductLearningImprovementThemeSqlRow
{
    public Guid ThemeId { get; init; }
    public Guid TenantId { get; init; }
    public Guid WorkspaceId { get; init; }
    public Guid ProjectId { get; init; }
    public string ThemeKey { get; init; } = string.Empty;
    public string? SourceAggregateKey { get; init; }
    public string? PatternKey { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Summary { get; init; } = string.Empty;
    public string AffectedArtifactTypeOrWorkflowArea { get; init; } = string.Empty;
    public string SeverityBand { get; init; } = string.Empty;
    public int EvidenceSignalCount { get; init; }
    public int DistinctRunCount { get; init; }
    public double? AverageTrustScore { get; init; }
    public string DerivationRuleVersion { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public DateTime CreatedUtc { get; init; }
    public string? CreatedByUserId { get; init; }
}

[ExcludeFromCodeCoverage(Justification = "Dapper row-mapping DTO with no logic.")]
internal sealed class ProductLearningImprovementPlanSqlRow
{
    public Guid PlanId { get; init; }
    public Guid TenantId { get; init; }
    public Guid WorkspaceId { get; init; }
    public Guid ProjectId { get; init; }
    public Guid ThemeId { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Summary { get; init; } = string.Empty;
    public string BoundedActionsJson { get; init; } = string.Empty;
    public int PriorityScore { get; init; }
    public string? PriorityExplanation { get; init; }
    public string Status { get; init; } = string.Empty;
    public DateTime CreatedUtc { get; init; }
    public string? CreatedByUserId { get; init; }
}

internal sealed class ProductLearningImprovementPlanSignalLinkSqlRow
{
    public Guid PlanId { get; init; }
    public Guid SignalId { get; init; }
    public string? TriageStatusSnapshot { get; init; }
}

[ExcludeFromCodeCoverage(Justification = "Dapper row-mapping DTO with no logic.")]
internal sealed class ProductLearningImprovementPlanArtifactLinkSqlRow
{
    public Guid LinkId { get; init; }
    public Guid PlanId { get; init; }
    public Guid? AuthorityBundleId { get; init; }
    public int? AuthorityArtifactSortOrder { get; init; }
    public string? PilotArtifactHint { get; init; }
}
