using System.Collections.Concurrent;

using ArchiForge.Contracts.Evolution;
using ArchiForge.Contracts.ProductLearning;

namespace ArchiForge.Persistence.Evolution;

/// <summary>In-memory 60R candidates for StorageProvider=InMemory.</summary>
public sealed class InMemoryEvolutionCandidateChangeSetRepository : IEvolutionCandidateChangeSetRepository
{
    private readonly ConcurrentDictionary<Guid, EvolutionCandidateChangeSetRecord> _byId = new();

    public Task InsertAsync(EvolutionCandidateChangeSetRecord record, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!_byId.TryAdd(record.CandidateChangeSetId, record))
        {
            throw new InvalidOperationException($"Candidate change set '{record.CandidateChangeSetId}' already exists.");
        }

        return Task.CompletedTask;
    }

    public Task<EvolutionCandidateChangeSetRecord?> GetByIdAsync(
        Guid candidateChangeSetId,
        ProductLearningScope scope,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!_byId.TryGetValue(candidateChangeSetId, out EvolutionCandidateChangeSetRecord? row))
        {
            return Task.FromResult<EvolutionCandidateChangeSetRecord?>(null);
        }

        if (row.TenantId != scope.TenantId ||
            row.WorkspaceId != scope.WorkspaceId ||
            row.ProjectId != scope.ProjectId)
        {
            return Task.FromResult<EvolutionCandidateChangeSetRecord?>(null);
        }

        return Task.FromResult<EvolutionCandidateChangeSetRecord?>(row);
    }

    public Task<IReadOnlyList<EvolutionCandidateChangeSetRecord>> ListAsync(
        ProductLearningScope scope,
        int take,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        int n = take < 1 ? 1 : Math.Min(take, 100);

        List<EvolutionCandidateChangeSetRecord> list = _byId.Values
            .Where(r =>
                r.TenantId == scope.TenantId &&
                r.WorkspaceId == scope.WorkspaceId &&
                r.ProjectId == scope.ProjectId)
            .OrderByDescending(static r => r.CreatedUtc)
            .ThenBy(static r => r.CandidateChangeSetId)
            .Take(n)
            .ToList();

        return Task.FromResult<IReadOnlyList<EvolutionCandidateChangeSetRecord>>(list);
    }

    public Task UpdateStatusAsync(
        Guid candidateChangeSetId,
        ProductLearningScope scope,
        string status,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!_byId.TryGetValue(candidateChangeSetId, out EvolutionCandidateChangeSetRecord? row))
        {
            return Task.CompletedTask;
        }

        if (row.TenantId != scope.TenantId ||
            row.WorkspaceId != scope.WorkspaceId ||
            row.ProjectId != scope.ProjectId)
        {
            return Task.CompletedTask;
        }

        EvolutionCandidateChangeSetRecord updated = new()
        {
            CandidateChangeSetId = row.CandidateChangeSetId,
            TenantId = row.TenantId,
            WorkspaceId = row.WorkspaceId,
            ProjectId = row.ProjectId,
            SourcePlanId = row.SourcePlanId,
            Status = status,
            Title = row.Title,
            Summary = row.Summary,
            PlanSnapshotJson = row.PlanSnapshotJson,
            DerivationRuleVersion = row.DerivationRuleVersion,
            CreatedUtc = row.CreatedUtc,
            CreatedByUserId = row.CreatedByUserId,
        };

        _byId[candidateChangeSetId] = updated;

        return Task.CompletedTask;
    }
}
