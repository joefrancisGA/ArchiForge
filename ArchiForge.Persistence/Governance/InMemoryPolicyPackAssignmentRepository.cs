using ArchiForge.Decisioning.Governance.PolicyPacks;
using ArchiForge.Decisioning.Governance.Resolution;

namespace ArchiForge.Persistence.Governance;

/// <summary>
/// Thread-safe in-memory store for <see cref="PolicyPackAssignment"/> used when <c>StorageProvider=InMemory</c> or in unit tests.
/// </summary>
/// <remarks>
/// <para>
/// <strong>List semantics:</strong> Must mirror <see cref="DapperPolicyPackAssignmentRepository.ListByScopeAsync"/> so Decisioning behavior is identical
/// between SQL and in-memory hosts.
/// </para>
/// <para>
/// Registered as singleton in in-memory storage bootstrap (see <c>ArchiForgeStorageServiceCollectionExtensions</c>).
/// </para>
/// </remarks>
public sealed class InMemoryPolicyPackAssignmentRepository : IPolicyPackAssignmentRepository
{
    private const int MaxEntries = 2_000;

    private readonly List<PolicyPackAssignment> _items = [];
    private readonly object _gate = new();

    /// <inheritdoc />
    public Task CreateAsync(PolicyPackAssignment assignment, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(assignment);
        ct.ThrowIfCancellationRequested();
        lock (_gate)
        {
            if (_items.Count >= MaxEntries)
                _items.RemoveAt(0);

            _items.Add(assignment);
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task UpdateAsync(PolicyPackAssignment assignment, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(assignment);
        ct.ThrowIfCancellationRequested();
        lock (_gate)
        {
            int i = _items.FindIndex(x => x.AssignmentId == assignment.AssignmentId);
            if (i >= 0)
                _items[i] = assignment;
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    /// <remarks>Excludes non-matching scope tiers; does not filter <see cref="PolicyPackAssignment.IsEnabled"/> here.</remarks>
    public Task<IReadOnlyList<PolicyPackAssignment>> ListByScopeAsync(
        Guid tenantId,
        Guid workspaceId,
        Guid projectId,
        CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        lock (_gate)
        {
            List<PolicyPackAssignment> result = _items
                .Where(x => x.TenantId == tenantId)
                .Where(x =>
                    string.Equals(x.ScopeLevel, GovernanceScopeLevel.Tenant, StringComparison.Ordinal) ||
                    (string.Equals(x.ScopeLevel, GovernanceScopeLevel.Workspace, StringComparison.Ordinal) &&
                     x.WorkspaceId == workspaceId) ||
                    (string.Equals(x.ScopeLevel, GovernanceScopeLevel.Project, StringComparison.Ordinal) &&
                     x.WorkspaceId == workspaceId && x.ProjectId == projectId))
                .OrderByDescending(x => x.AssignedUtc)
                .ToList();
            return Task.FromResult<IReadOnlyList<PolicyPackAssignment>>(result);
        }
    }
}
