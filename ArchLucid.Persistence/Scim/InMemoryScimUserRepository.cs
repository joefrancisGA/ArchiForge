using System.Collections.Concurrent;

using ArchLucid.Core.Scim;
using ArchLucid.Core.Scim.Filtering;
using ArchLucid.Core.Scim.Models;

namespace ArchLucid.Persistence.Scim;

public sealed class InMemoryScimUserRepository : IScimUserRepository
{
    private readonly ConcurrentDictionary<Guid, ScimUserRecord> _byId = new();

    /// <inheritdoc />
    public Task<(IReadOnlyList<ScimUserRecord> items, int totalCount)> ListAsync(
        Guid tenantId,
        ScimFilterNode? filter,
        int startIndex1Based,
        int count,
        CancellationToken cancellationToken)
    {
        _ = cancellationToken;

        List<ScimUserRecord> all = _byId.Values.Where(u => u.TenantId == tenantId).ToList();
        IEnumerable<ScimUserRecord> filtered = all.Where(u => ScimFilterInMemoryEvaluator.Matches(u, filter)).ToList();
        int total = filtered.Count();
        int offset = Math.Max(0, startIndex1Based - 1);
        List<ScimUserRecord> page = filtered.Skip(offset).Take(Math.Clamp(count, 0, 200)).ToList();

        return Task.FromResult<(IReadOnlyList<ScimUserRecord> items, int totalCount)>((page, total));
    }

    /// <inheritdoc />
    public Task<ScimUserRecord?> GetByIdAsync(Guid tenantId, Guid id, CancellationToken cancellationToken)
    {
        _ = cancellationToken;

        return Task.FromResult(
            _byId.TryGetValue(id, out ScimUserRecord? u) && u.TenantId == tenantId ? u : null);
    }

    /// <inheritdoc />
    public Task<ScimUserRecord?> GetByExternalIdAsync(Guid tenantId, string externalId, CancellationToken cancellationToken)
    {
        _ = cancellationToken;

        ScimUserRecord? found = _byId.Values.FirstOrDefault(u =>
            u.TenantId == tenantId && string.Equals(u.ExternalId, externalId, StringComparison.OrdinalIgnoreCase));

        return Task.FromResult(found);
    }

    /// <inheritdoc />
    public Task<ScimUserRecord> InsertAsync(
        Guid tenantId,
        string externalId,
        string userName,
        string? displayName,
        bool active,
        string? resolvedRole,
        ScimResolvedRoleOrigin resolvedRoleOrigin,
        CancellationToken cancellationToken)
    {
        _ = cancellationToken;

        DateTimeOffset now = DateTimeOffset.UtcNow;
        ScimUserRecord u = new()
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ExternalId = externalId,
            UserName = userName,
            DisplayName = displayName,
            Active = active,
            ResolvedRole = resolvedRole,
            ResolvedRoleOrigin = resolvedRoleOrigin,
            CreatedUtc = now,
            UpdatedUtc = now
        };

        _byId[u.Id] = u;

        return Task.FromResult(u);
    }

    /// <inheritdoc />
    public Task ReplaceAsync(
        Guid tenantId,
        Guid id,
        string externalId,
        string userName,
        string? displayName,
        bool active,
        string? resolvedRole,
        ScimResolvedRoleOrigin resolvedRoleOrigin,
        CancellationToken cancellationToken)
    {
        _ = cancellationToken;

        if (!_byId.TryGetValue(id, out ScimUserRecord? existing) || existing.TenantId != tenantId)
            return Task.CompletedTask;


        DateTimeOffset now = DateTimeOffset.UtcNow;
        _byId[id] = new ScimUserRecord
        {
            Id = id,
            TenantId = tenantId,
            ExternalId = externalId,
            UserName = userName,
            DisplayName = displayName,
            Active = active,
            ResolvedRole = resolvedRole,
            ResolvedRoleOrigin = resolvedRoleOrigin,
            CreatedUtc = existing.CreatedUtc,
            UpdatedUtc = now
        };

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task PatchAsync(
        Guid tenantId,
        Guid id,
        string? externalId,
        string? userName,
        string? displayName,
        bool? active,
        string? resolvedRole,
        ScimResolvedRoleOrigin resolvedRoleOrigin,
        CancellationToken cancellationToken)
    {
        _ = cancellationToken;

        if (!_byId.TryGetValue(id, out ScimUserRecord? e) || e.TenantId != tenantId)
            return Task.CompletedTask;


        ScimUserRecord u = new()
        {
            Id = id,
            TenantId = tenantId,
            ExternalId = externalId ?? e.ExternalId,
            UserName = userName ?? e.UserName,
            DisplayName = displayName ?? e.DisplayName,
            Active = active ?? e.Active,
            ResolvedRole = resolvedRole ?? e.ResolvedRole,
            ResolvedRoleOrigin = resolvedRoleOrigin,
            CreatedUtc = e.CreatedUtc,
            UpdatedUtc = DateTimeOffset.UtcNow
        };

        _byId[id] = u;

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task DeactivateAsync(Guid tenantId, Guid id, CancellationToken cancellationToken)
    {
        _ = cancellationToken;

        if (!_byId.TryGetValue(id, out ScimUserRecord? e) || e.TenantId != tenantId)
            return Task.CompletedTask;


        _byId[id] = new ScimUserRecord
        {
            Id = id,
            TenantId = tenantId,
            ExternalId = e.ExternalId,
            UserName = e.UserName,
            DisplayName = e.DisplayName,
            Active = false,
            ResolvedRole = e.ResolvedRole,
            ResolvedRoleOrigin = e.ResolvedRoleOrigin,
            CreatedUtc = e.CreatedUtc,
            UpdatedUtc = DateTimeOffset.UtcNow
        };

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<(string DisplayName, string ExternalId)>> ListGroupKeysForUserAsync(
        Guid tenantId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        _ = cancellationToken;

        return Task.FromResult<IReadOnlyList<(string DisplayName, string ExternalId)>>([]);
    }
}
