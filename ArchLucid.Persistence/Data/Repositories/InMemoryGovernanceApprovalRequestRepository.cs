using System.Text.Json;

using ArchLucid.Contracts.Common;
using ArchLucid.Contracts.Governance;

namespace ArchLucid.Persistence.Data.Repositories;

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

    /// <inheritdoc />
    public Task<IReadOnlyList<GovernanceApprovalRequest>> GetPendingAsync(
        int maxRows = 50,
        CancellationToken cancellationToken = default)
    {
        if (maxRows <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxRows));
        }

        cancellationToken.ThrowIfCancellationRequested();

        lock (_gate)
        {
            List<GovernanceApprovalRequest> ordered = _byId.Values
                .Where(
                    x => string.Equals(x.Status, GovernanceApprovalStatus.Draft, StringComparison.Ordinal)
                         || string.Equals(x.Status, GovernanceApprovalStatus.Submitted, StringComparison.Ordinal))
                .OrderByDescending(x => x.RequestedUtc)
                .Take(maxRows)
                .Select(Clone)
                .ToList();

            return Task.FromResult<IReadOnlyList<GovernanceApprovalRequest>>(ordered);
        }
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<GovernanceApprovalRequest>> GetRecentDecisionsAsync(
        int maxRows = 50,
        CancellationToken cancellationToken = default)
    {
        if (maxRows <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxRows));
        }

        cancellationToken.ThrowIfCancellationRequested();

        lock (_gate)
        {
            List<GovernanceApprovalRequest> ordered = _byId.Values
                .Where(
                    x => x.ReviewedUtc.HasValue
                         && (string.Equals(x.Status, GovernanceApprovalStatus.Approved, StringComparison.Ordinal)
                             || string.Equals(x.Status, GovernanceApprovalStatus.Rejected, StringComparison.Ordinal)
                             || string.Equals(x.Status, GovernanceApprovalStatus.Promoted, StringComparison.Ordinal)))
                .OrderByDescending(x => x.ReviewedUtc)
                .Take(maxRows)
                .Select(Clone)
                .ToList();

            return Task.FromResult<IReadOnlyList<GovernanceApprovalRequest>>(ordered);
        }
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<GovernanceApprovalRequest>> GetPendingSlaBreachedAsync(
        DateTime utcNow,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        lock (_gate)
        {
            List<GovernanceApprovalRequest> breached = _byId.Values
                .Where(
                    x => (string.Equals(x.Status, GovernanceApprovalStatus.Draft, StringComparison.Ordinal)
                          || string.Equals(x.Status, GovernanceApprovalStatus.Submitted, StringComparison.Ordinal))
                         && x.SlaDeadlineUtc.HasValue
                         && x.SlaDeadlineUtc.Value <= utcNow
                         && !x.SlaBreachNotifiedUtc.HasValue)
                .OrderBy(x => x.SlaDeadlineUtc)
                .Select(Clone)
                .ToList();

            return Task.FromResult<IReadOnlyList<GovernanceApprovalRequest>>(breached);
        }
    }

    /// <inheritdoc />
    public Task PatchSlaBreachNotifiedAsync(
        string approvalRequestId,
        DateTime slaBreachNotifiedUtc,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(approvalRequestId);
        cancellationToken.ThrowIfCancellationRequested();

        lock (_gate)
        {
            if (_byId.TryGetValue(approvalRequestId, out GovernanceApprovalRequest? row))
            {
                row.SlaBreachNotifiedUtc = slaBreachNotifiedUtc;
            }
        }

        return Task.CompletedTask;
    }

    private static GovernanceApprovalRequest Clone(GovernanceApprovalRequest source)
    {
        string json = JsonSerializer.Serialize(source, ContractJson.Default);
        GovernanceApprovalRequest? copy = JsonSerializer.Deserialize<GovernanceApprovalRequest>(json, ContractJson.Default);

        return copy ?? throw new InvalidOperationException("Clone produced null GovernanceApprovalRequest.");
    }
}
