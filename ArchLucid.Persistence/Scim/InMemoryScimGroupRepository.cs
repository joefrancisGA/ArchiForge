using System.Collections.Concurrent;

using ArchLucid.Core.Scim;
using ArchLucid.Core.Scim.Models;

namespace ArchLucid.Persistence.Scim;

public sealed class InMemoryScimGroupRepository : IScimGroupRepository
{
    private readonly ConcurrentDictionary<Guid, ScimGroupRecord> _byId = new();

    private readonly ConcurrentDictionary<(Guid TenantId, Guid UserId, Guid GroupId), byte> _members = new();

    /// <inheritdoc />
    public Task<(IReadOnlyList<ScimGroupRecord> items, int totalCount)> ListAsync(
        Guid tenantId,
        int startIndex1Based,
        int count,
        CancellationToken cancellationToken)
    {
        _ = cancellationToken;

        List<ScimGroupRecord> all = _byId.Values.Where(g => g.TenantId == tenantId).OrderBy(static g => g.CreatedUtc).ToList();
        int total = all.Count;
        int offset = Math.Max(0, startIndex1Based - 1);
        List<ScimGroupRecord> page = all.Skip(offset).Take(Math.Clamp(count, 0, 200)).ToList();

        return Task.FromResult<(IReadOnlyList<ScimGroupRecord> items, int totalCount)>((page, total));
    }

    /// <inheritdoc />
    public Task<ScimGroupRecord?> GetByIdAsync(Guid tenantId, Guid id, CancellationToken cancellationToken)
    {
        _ = cancellationToken;

        return Task.FromResult(
            _byId.TryGetValue(id, out ScimGroupRecord? g) && g.TenantId == tenantId ? g : null);
    }

    /// <inheritdoc />
    public Task<ScimGroupRecord> InsertAsync(
        Guid tenantId,
        string externalId,
        string displayName,
        CancellationToken cancellationToken)
    {
        _ = cancellationToken;

        DateTimeOffset now = DateTimeOffset.UtcNow;
        ScimGroupRecord g = new()
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ExternalId = externalId,
            DisplayName = displayName,
            CreatedUtc = now,
            UpdatedUtc = now
        };

        _byId[g.Id] = g;

        return Task.FromResult(g);
    }

    /// <inheritdoc />
    public Task ReplaceAsync(
        Guid tenantId,
        Guid id,
        string externalId,
        string displayName,
        CancellationToken cancellationToken)
    {
        _ = cancellationToken;

        if (!_byId.TryGetValue(id, out ScimGroupRecord? e) || e.TenantId != tenantId)
            return Task.CompletedTask;


        DateTimeOffset now = DateTimeOffset.UtcNow;
        _byId[id] = new ScimGroupRecord
        {
            Id = id,
            TenantId = tenantId,
            ExternalId = externalId,
            DisplayName = displayName,
            CreatedUtc = e.CreatedUtc,
            UpdatedUtc = now
        };

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task SetMembersAsync(Guid tenantId, Guid groupId, IReadOnlyList<Guid> userIds, CancellationToken cancellationToken)
    {
        _ = cancellationToken;

        foreach (KeyValuePair<(Guid TenantId, Guid UserId, Guid GroupId), byte> kv in _members.ToArray())
        {
            if (kv.Key.TenantId == tenantId && kv.Key.GroupId == groupId)
                _members.TryRemove(kv.Key, out _);
        }

        foreach (Guid userId in userIds)
            _members.TryAdd((tenantId, userId, groupId), 0);

        return Task.CompletedTask;
    }
}
