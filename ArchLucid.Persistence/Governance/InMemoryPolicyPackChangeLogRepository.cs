using System.Data;

using ArchLucid.Contracts.Governance;
using ArchLucid.Decisioning.Governance.PolicyPacks;

namespace ArchLucid.Persistence.Governance;

/// <summary>
///     Thread-safe in-memory append-only log for <see cref="PolicyPackChangeLogEntry" />.
/// </summary>
public sealed class InMemoryPolicyPackChangeLogRepository : IPolicyPackChangeLogRepository
{
    private const int MaxEntries = 10_000;
    private readonly Lock _gate = new();

    private readonly List<PolicyPackChangeLogEntry> _items = [];

    /// <inheritdoc />
    public Task AppendAsync(
        PolicyPackChangeLogEntry entry,
        CancellationToken cancellationToken = default,
        IDbConnection? connection = null,
        IDbTransaction? transaction = null)
    {
        ArgumentNullException.ThrowIfNull(entry);
        ArgumentException.ThrowIfNullOrWhiteSpace(entry.ChangeType);
        ArgumentException.ThrowIfNullOrWhiteSpace(entry.ChangedBy);
        cancellationToken.ThrowIfCancellationRequested();

        DateTime changedUtc = entry.ChangedUtc == default ? DateTime.UtcNow : entry.ChangedUtc;
        Guid changeLogId = entry.ChangeLogId == Guid.Empty ? Guid.NewGuid() : entry.ChangeLogId;

        PolicyPackChangeLogEntry stored = new()
        {
            ChangeLogId = changeLogId,
            PolicyPackId = entry.PolicyPackId,
            TenantId = entry.TenantId,
            WorkspaceId = entry.WorkspaceId,
            ProjectId = entry.ProjectId,
            ChangeType = entry.ChangeType,
            ChangedBy = entry.ChangedBy,
            ChangedUtc = changedUtc,
            PreviousValue = entry.PreviousValue,
            NewValue = entry.NewValue,
            SummaryText = entry.SummaryText
        };

        lock (_gate)
        {
            if (_items.Count >= MaxEntries)

                _items.RemoveAt(0);


            _items.Add(stored);
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<PolicyPackChangeLogEntry>> GetByPolicyPackIdAsync(
        Guid policyPackId,
        int maxRows = 50,
        CancellationToken cancellationToken = default)
    {
        if (maxRows <= 0)
            throw new ArgumentOutOfRangeException(nameof(maxRows));


        cancellationToken.ThrowIfCancellationRequested();
        lock (_gate)
        {
            List<PolicyPackChangeLogEntry> result = _items
                .Where(e => e.PolicyPackId == policyPackId)
                .OrderByDescending(e => e.ChangedUtc)
                .Take(maxRows)
                .ToList();

            return Task.FromResult<IReadOnlyList<PolicyPackChangeLogEntry>>(result);
        }
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<PolicyPackChangeLogEntry>> GetByTenantAsync(
        Guid tenantId,
        int maxRows = 100,
        CancellationToken cancellationToken = default)
    {
        if (maxRows <= 0)
            throw new ArgumentOutOfRangeException(nameof(maxRows));


        cancellationToken.ThrowIfCancellationRequested();
        lock (_gate)
        {
            List<PolicyPackChangeLogEntry> result = _items
                .Where(e => e.TenantId == tenantId)
                .OrderByDescending(e => e.ChangedUtc)
                .Take(maxRows)
                .ToList();

            return Task.FromResult<IReadOnlyList<PolicyPackChangeLogEntry>>(result);
        }
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<PolicyPackChangeLogEntry>> GetByTenantInRangeAsync(
        Guid tenantId,
        DateTime fromUtc,
        DateTime toUtc,
        CancellationToken cancellationToken = default)
    {
        if (fromUtc >= toUtc)
            throw new ArgumentOutOfRangeException(nameof(toUtc), "toUtc must be greater than fromUtc.");


        cancellationToken.ThrowIfCancellationRequested();
        lock (_gate)
        {
            List<PolicyPackChangeLogEntry> result = _items
                .Where(e => e.TenantId == tenantId && e.ChangedUtc >= fromUtc && e.ChangedUtc < toUtc)
                .OrderBy(e => e.ChangedUtc)
                .ToList();

            return Task.FromResult<IReadOnlyList<PolicyPackChangeLogEntry>>(result);
        }
    }
}
