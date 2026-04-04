using System.Text.Json;

using ArchiForge.Contracts.Common;
using ArchiForge.Contracts.Governance;

namespace ArchiForge.Persistence.Data.Repositories;

/// <summary>
/// Thread-safe in-memory <see cref="IGovernanceApprovalRequestRepository"/> (JSON clone-on-read).
/// </summary>
public sealed class InMemoryGovernanceApprovalRequestRepository : IGovernanceApprovalRequestRepository
{
    private const int MaxListRows = 200;
    private readonly Dictionary<string, GovernanceApprovalRequest> _byId = new(StringComparer.Ordinal);
    private readonly Lock _gate = new();

    /// <inheritdoc />
    public Task CreateAsync(GovernanceApprovalRequest item, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(item);
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(item.ApprovalRequestId))
        
            throw new ArgumentException("ApprovalRequestId is required.", nameof(item));
        

        GovernanceApprovalRequest stored = Clone(item);

        lock (_gate)
        
            _byId[stored.ApprovalRequestId] = stored;
        

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task UpdateAsync(GovernanceApprovalRequest item, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(item);
        cancellationToken.ThrowIfCancellationRequested();

        lock (_gate)
        {
            if (!_byId.ContainsKey(item.ApprovalRequestId))
            
                return Task.CompletedTask;
            

            _byId[item.ApprovalRequestId] = Clone(item);
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<GovernanceApprovalRequest?> GetByIdAsync(
        string approvalRequestId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        lock (_gate)
        
            return Task.FromResult(
                _byId.TryGetValue(approvalRequestId, out GovernanceApprovalRequest? row) ? Clone(row) : null);
        
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<GovernanceApprovalRequest>> GetByRunIdAsync(
        string runId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        lock (_gate)
        {
            List<GovernanceApprovalRequest> ordered = _byId.Values
                .Where(x => string.Equals(x.RunId, runId, StringComparison.Ordinal))
                .OrderByDescending(x => x.RequestedUtc)
                .Take(MaxListRows)
                .Select(Clone)
                .ToList();

            return Task.FromResult<IReadOnlyList<GovernanceApprovalRequest>>(ordered);
        }
    }

    private static GovernanceApprovalRequest Clone(GovernanceApprovalRequest source)
    {
        string json = JsonSerializer.Serialize(source, ContractJson.Default);
        GovernanceApprovalRequest? copy = JsonSerializer.Deserialize<GovernanceApprovalRequest>(json, ContractJson.Default);

        return copy ?? throw new InvalidOperationException("Clone produced null GovernanceApprovalRequest.");
    }
}
