using ArchiForge.Contracts.ProductLearning;

namespace ArchiForge.Persistence.ProductLearning;

/// <summary>
/// Persistence for <see cref="ProductLearningPilotSignalRecord"/> rows (SQL Server via Dapper in production).
/// </summary>
public interface IProductLearningPilotSignalRepository
{
    /// <summary>Inserts a signal. Assigns <see cref="ProductLearningPilotSignalRecord.SignalId"/> and <see cref="ProductLearningPilotSignalRecord.RecordedUtc"/> when unset/default.</summary>
    Task InsertAsync(ProductLearningPilotSignalRecord record, CancellationToken cancellationToken);

    /// <summary>Latest signals for a tenant/workspace/project scope, newest first.</summary>
    Task<IReadOnlyList<ProductLearningPilotSignalRecord>> ListRecentForScopeAsync(
        Guid tenantId,
        Guid workspaceId,
        Guid projectId,
        int take,
        CancellationToken cancellationToken);

    /// <summary>
    /// Run-level feedback rollups (pattern key when set, else subject+artifact composite key).
    /// Ordering: newest activity first, then <see cref="FeedbackAggregate.AggregateKey"/> ascending.
    /// </summary>
    Task<IReadOnlyList<FeedbackAggregate>> ListRunFeedbackAggregatesAsync(
        Guid tenantId,
        Guid workspaceId,
        Guid projectId,
        DateTime? sinceUtc,
        int maxAggregates,
        CancellationToken cancellationToken);

    /// <summary>Artifact-facet outcome mix (subject + artifact hint), practical window label supplied by caller.</summary>
    Task<IReadOnlyList<ArtifactOutcomeTrend>> ListArtifactOutcomeTrendsAsync(
        Guid tenantId,
        Guid workspaceId,
        Guid projectId,
        DateTime? sinceUtc,
        string? windowLabel,
        int maxTrends,
        CancellationToken cancellationToken);

    /// <summary>Rollups with the highest rejected+revised volume (noise filter for triage).</summary>
    Task<IReadOnlyList<FeedbackAggregate>> ListTopRejectedRevisedArtifactRollupsAsync(
        Guid tenantId,
        Guid workspaceId,
        Guid projectId,
        DateTime? sinceUtc,
        int take,
        CancellationToken cancellationToken);

    /// <summary>
    /// Repeated leading text in comments (trimmed, first 200 chars). Deterministic; not semantic tagging.
    /// </summary>
    Task<IReadOnlyList<RepeatedCommentTheme>> ListRepeatedCommentThemesAsync(
        Guid tenantId,
        Guid workspaceId,
        Guid projectId,
        DateTime? sinceUtc,
        int minOccurrences,
        int take,
        CancellationToken cancellationToken);

    /// <summary>
    /// Candidates derived from aggregates with repeated poor outcomes (reject/follow-up) or heavy revision churn.
    /// </summary>
    Task<IReadOnlyList<ImprovementOpportunity>> ListImprovementOpportunityCandidatesAsync(
        Guid tenantId,
        Guid workspaceId,
        Guid projectId,
        DateTime? sinceUtc,
        int minPoorOutcomeSignals,
        int minRevisedSignals,
        int take,
        CancellationToken cancellationToken);
}
