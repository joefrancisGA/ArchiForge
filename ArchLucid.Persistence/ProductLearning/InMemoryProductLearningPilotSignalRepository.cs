using ArchiForge.Contracts.ProductLearning;

namespace ArchiForge.Persistence.ProductLearning;

/// <summary>In-memory store for Development / tests (deterministic ordering by <see cref="ProductLearningPilotSignalRecord.RecordedUtc"/> desc).</summary>
public sealed class InMemoryProductLearningPilotSignalRepository : IProductLearningPilotSignalRepository
{
    private readonly object _sync = new();

    private readonly List<ProductLearningPilotSignalRecord> _rows = [];

    public Task InsertAsync(ProductLearningPilotSignalRecord record, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(record);

        if (string.IsNullOrWhiteSpace(record.SubjectType))
        
            throw new ArgumentException("SubjectType is required.", nameof(record));
        

        if (string.IsNullOrWhiteSpace(record.Disposition))
        
            throw new ArgumentException("Disposition is required.", nameof(record));
        

        Guid signalId = record.SignalId == Guid.Empty ? Guid.NewGuid() : record.SignalId;
        DateTime recordedUtc = record.RecordedUtc == default ? DateTime.UtcNow : record.RecordedUtc;
        string triage = string.IsNullOrWhiteSpace(record.TriageStatus)
            ? ProductLearningTriageStatusValues.Open
            : record.TriageStatus;

        ProductLearningPilotSignalRecord stored = record with
        {
            SignalId = signalId,
            RecordedUtc = recordedUtc,
            TriageStatus = triage,
        };

        lock (_sync)
        
            _rows.Add(stored);
        

        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<ProductLearningPilotSignalRecord>> ListRecentForScopeAsync(
        Guid tenantId,
        Guid workspaceId,
        Guid projectId,
        int take,
        CancellationToken cancellationToken)
    {
        int capped = take < 1 ? 1 : Math.Min(take, 500);

        List<ProductLearningPilotSignalRecord> list;

        lock (_sync)
        
            list = _rows
                .Where(r =>
                    r.TenantId == tenantId &&
                    r.WorkspaceId == workspaceId &&
                    r.ProjectId == projectId)
                .OrderByDescending(static r => r.RecordedUtc)
                .ThenBy(static r => r.SignalId)
                .Take(capped)
                .Select(static r => r with { })
                .ToList();
        

        return Task.FromResult<IReadOnlyList<ProductLearningPilotSignalRecord>>(list);
    }

    public Task<IReadOnlyList<FeedbackAggregate>> ListRunFeedbackAggregatesAsync(
        Guid tenantId,
        Guid workspaceId,
        Guid projectId,
        DateTime? sinceUtc,
        int maxAggregates,
        CancellationToken cancellationToken)
    {
        IEnumerable<ProductLearningPilotSignalRecord> scoped = ProductLearningSignalAggregations.FilterScope(
            SnapshotRows(),
            tenantId,
            workspaceId,
            projectId,
            sinceUtc);

        IReadOnlyList<FeedbackAggregate> list =
            ProductLearningSignalAggregations.BuildRunFeedbackAggregates(scoped, maxAggregates);

        return Task.FromResult(list);
    }

    public Task<IReadOnlyList<ArtifactOutcomeTrend>> ListArtifactOutcomeTrendsAsync(
        Guid tenantId,
        Guid workspaceId,
        Guid projectId,
        DateTime? sinceUtc,
        string? windowLabel,
        int maxTrends,
        CancellationToken cancellationToken)
    {
        IEnumerable<ProductLearningPilotSignalRecord> scoped = ProductLearningSignalAggregations.FilterScope(
            SnapshotRows(),
            tenantId,
            workspaceId,
            projectId,
            sinceUtc);

        IReadOnlyList<ArtifactOutcomeTrend> list =
            ProductLearningSignalAggregations.BuildArtifactOutcomeTrends(scoped, windowLabel, maxTrends);

        return Task.FromResult(list);
    }

    public Task<IReadOnlyList<FeedbackAggregate>> ListTopRejectedRevisedArtifactRollupsAsync(
        Guid tenantId,
        Guid workspaceId,
        Guid projectId,
        DateTime? sinceUtc,
        int take,
        CancellationToken cancellationToken)
    {
        IEnumerable<ProductLearningPilotSignalRecord> scoped = ProductLearningSignalAggregations.FilterScope(
            SnapshotRows(),
            tenantId,
            workspaceId,
            projectId,
            sinceUtc);

        IReadOnlyList<FeedbackAggregate> list =
            ProductLearningSignalAggregations.BuildTopRejectedRevisedRollups(scoped, take);

        return Task.FromResult(list);
    }

    public Task<IReadOnlyList<RepeatedCommentTheme>> ListRepeatedCommentThemesAsync(
        Guid tenantId,
        Guid workspaceId,
        Guid projectId,
        DateTime? sinceUtc,
        int minOccurrences,
        int take,
        CancellationToken cancellationToken)
    {
        IEnumerable<ProductLearningPilotSignalRecord> scoped = ProductLearningSignalAggregations.FilterScope(
            SnapshotRows(),
            tenantId,
            workspaceId,
            projectId,
            sinceUtc);

        IReadOnlyList<RepeatedCommentTheme> list =
            ProductLearningSignalAggregations.BuildRepeatedCommentThemes(scoped, minOccurrences, take);

        return Task.FromResult(list);
    }

    public Task<IReadOnlyList<ImprovementOpportunity>> ListImprovementOpportunityCandidatesAsync(
        Guid tenantId,
        Guid workspaceId,
        Guid projectId,
        DateTime? sinceUtc,
        int minPoorOutcomeSignals,
        int minRevisedSignals,
        int take,
        CancellationToken cancellationToken)
    {
        IEnumerable<ProductLearningPilotSignalRecord> scoped = ProductLearningSignalAggregations.FilterScope(
            SnapshotRows(),
            tenantId,
            workspaceId,
            projectId,
            sinceUtc);

        IReadOnlyList<ImprovementOpportunity> list = ProductLearningSignalAggregations.BuildImprovementOpportunityCandidates(
            scoped,
            minPoorOutcomeSignals,
            minRevisedSignals,
            take);

        return Task.FromResult(list);
    }

    private IReadOnlyList<ProductLearningPilotSignalRecord> SnapshotRows()
    {
        lock (_sync)
        
            return _rows.Select(static r => r with { }).ToList();
        
    }

    public Task<int> CountSignalsInScopeAsync(
        Guid tenantId,
        Guid workspaceId,
        Guid projectId,
        DateTime? sinceUtc,
        CancellationToken cancellationToken)
    {
        IEnumerable<ProductLearningPilotSignalRecord> scoped = ProductLearningSignalAggregations.FilterScope(
            SnapshotRows(),
            tenantId,
            workspaceId,
            projectId,
            sinceUtc);

        return Task.FromResult(scoped.Count());
    }

    public Task<int> CountDistinctArchitectureRunsWithSignalsAsync(
        Guid tenantId,
        Guid workspaceId,
        Guid projectId,
        DateTime? sinceUtc,
        CancellationToken cancellationToken)
    {
        IEnumerable<ProductLearningPilotSignalRecord> scoped = ProductLearningSignalAggregations.FilterScope(
            SnapshotRows(),
            tenantId,
            workspaceId,
            projectId,
            sinceUtc);

        int n = scoped
            .Where(static r => !string.IsNullOrWhiteSpace(r.ArchitectureRunId))
            .Select(static r => r.ArchitectureRunId!)
            .Distinct(StringComparer.Ordinal)
            .Count();

        return Task.FromResult(n);
    }
}
