using System.Collections.Concurrent;

using ArchLucid.Core.Tenancy;

namespace ArchLucid.Persistence.Tenancy;

/// <summary>In-memory tenant registry for tests and <c>InMemory</c> storage mode.</summary>
public sealed class InMemoryTenantRepository : ITenantRepository
{
    private readonly ConcurrentDictionary<Guid, TenantRecord> _byId = new();

    private readonly ConcurrentDictionary<string, Guid> _slugToId = new(StringComparer.OrdinalIgnoreCase);

    private readonly ConcurrentDictionary<Guid, List<TenantWorkspaceRow>> _workspacesByTenant = new();

    private readonly ConcurrentDictionary<Guid, Guid> _entraTenantIdToTenantId = new();

    public Task<TenantRecord?> GetByIdAsync(Guid tenantId, CancellationToken ct)
    {
        _ = ct;

        return Task.FromResult(_byId.TryGetValue(tenantId, out TenantRecord? r) ? r : null);
    }

    public Task<TenantRecord?> GetBySlugAsync(string slug, CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(slug);
        _ = ct;

        string key = slug.Trim().ToLowerInvariant();

        if (!_slugToId.TryGetValue(key, out Guid id))
            return Task.FromResult<TenantRecord?>(null);

        return GetByIdAsync(id, ct);
    }

    public Task<TenantRecord?> GetByEntraTenantIdAsync(Guid entraTenantId, CancellationToken ct)
    {
        _ = ct;

        if (!_entraTenantIdToTenantId.TryGetValue(entraTenantId, out Guid tenantId))
            return Task.FromResult<TenantRecord?>(null);

        return GetByIdAsync(tenantId, ct);
    }

    public Task<IReadOnlyList<TenantRecord>> ListAsync(CancellationToken ct)
    {
        _ = ct;

        IReadOnlyList<TenantRecord> list = _byId.Values.OrderByDescending(static r => r.CreatedUtc).ToList();

        return Task.FromResult(list);
    }

    public Task InsertTenantAsync(
        Guid tenantId,
        string name,
        string slug,
        TenantTier tier,
        Guid? entraTenantId,
        CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(slug);
        _ = ct;

        string slugKey = slug.Trim().ToLowerInvariant();

        TenantRecord record = new()
        {
            Id = tenantId,
            Name = name,
            Slug = slugKey,
            Tier = tier,
            EntraTenantId = entraTenantId,
            CreatedUtc = DateTimeOffset.UtcNow,
            SuspendedUtc = null,
            TrialStartUtc = null,
            TrialExpiresUtc = null,
            TrialRunsLimit = null,
            TrialRunsUsed = 0,
            TrialSeatsLimit = null,
            TrialSeatsUsed = 1,
            TrialStatus = null,
            TrialSampleRunId = null,
        };

        if (!_byId.TryAdd(tenantId, record))
            throw new InvalidOperationException($"Tenant id '{tenantId:D}' already exists.");

        if (!_slugToId.TryAdd(slugKey, tenantId))
        {
            _byId.TryRemove(tenantId, out _);
            throw new InvalidOperationException($"Tenant slug '{slugKey}' already exists.");
        }

        if (entraTenantId.HasValue)
        {
            if (!_entraTenantIdToTenantId.TryAdd(entraTenantId.Value, tenantId))
            {
                _slugToId.TryRemove(slugKey, out _);
                _byId.TryRemove(tenantId, out _);
                throw new InvalidOperationException($"Entra tenant id '{entraTenantId.Value:D}' is already linked.");
            }
        }

        _workspacesByTenant.TryAdd(tenantId, new List<TenantWorkspaceRow>());

        return Task.CompletedTask;
    }

    public Task InsertWorkspaceAsync(
        Guid workspaceId,
        Guid tenantId,
        string name,
        Guid defaultProjectId,
        CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        _ = ct;

        List<TenantWorkspaceRow> list = _workspacesByTenant.GetOrAdd(tenantId, static _ => new List<TenantWorkspaceRow>());

        lock (list)
        {
            list.Add(
                new TenantWorkspaceRow
                {
                    Id = workspaceId,
                    TenantId = tenantId,
                    Name = name,
                    DefaultProjectId = defaultProjectId,
                    CreatedUtc = DateTimeOffset.UtcNow,
                });
        }

        return Task.CompletedTask;
    }

    public Task SuspendTenantAsync(Guid tenantId, CancellationToken ct)
    {
        _ = ct;

        if (!_byId.TryGetValue(tenantId, out TenantRecord? existing))
            return Task.CompletedTask;

        TenantRecord updated = new()
        {
            Id = existing.Id,
            Name = existing.Name,
            Slug = existing.Slug,
            Tier = existing.Tier,
            EntraTenantId = existing.EntraTenantId,
            CreatedUtc = existing.CreatedUtc,
            SuspendedUtc = DateTimeOffset.UtcNow,
            TrialStartUtc = existing.TrialStartUtc,
            TrialExpiresUtc = existing.TrialExpiresUtc,
            TrialRunsLimit = existing.TrialRunsLimit,
            TrialRunsUsed = existing.TrialRunsUsed,
            TrialSeatsLimit = existing.TrialSeatsLimit,
            TrialSeatsUsed = existing.TrialSeatsUsed,
            TrialStatus = existing.TrialStatus,
            TrialSampleRunId = existing.TrialSampleRunId,
        };

        _byId[tenantId] = updated;

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task CommitSelfServiceTrialAsync(
        Guid tenantId,
        DateTimeOffset trialStartUtc,
        DateTimeOffset trialExpiresUtc,
        int runsLimit,
        int seatsLimit,
        Guid sampleRunId,
        CancellationToken ct)
    {
        _ = ct;

        if (!_byId.TryGetValue(tenantId, out TenantRecord? existing))
            return Task.CompletedTask;

        TenantRecord updated = new()
        {
            Id = existing.Id,
            Name = existing.Name,
            Slug = existing.Slug,
            Tier = existing.Tier,
            EntraTenantId = existing.EntraTenantId,
            CreatedUtc = existing.CreatedUtc,
            SuspendedUtc = existing.SuspendedUtc,
            TrialStartUtc = trialStartUtc,
            TrialExpiresUtc = trialExpiresUtc,
            TrialRunsLimit = runsLimit,
            TrialRunsUsed = 0,
            TrialSeatsLimit = seatsLimit,
            TrialSeatsUsed = 1,
            TrialStatus = TrialLifecycleStatus.Active,
            TrialSampleRunId = sampleRunId,
        };

        _byId[tenantId] = updated;

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task MarkTrialConvertedAsync(Guid tenantId, CancellationToken ct)
    {
        _ = ct;

        if (!_byId.TryGetValue(tenantId, out TenantRecord? existing))
            return Task.CompletedTask;

        if (!string.Equals(existing.TrialStatus, TrialLifecycleStatus.Active, StringComparison.Ordinal))
            return Task.CompletedTask;

        TenantRecord updated = new()
        {
            Id = existing.Id,
            Name = existing.Name,
            Slug = existing.Slug,
            Tier = existing.Tier,
            EntraTenantId = existing.EntraTenantId,
            CreatedUtc = existing.CreatedUtc,
            SuspendedUtc = existing.SuspendedUtc,
            TrialStartUtc = existing.TrialStartUtc,
            TrialExpiresUtc = existing.TrialExpiresUtc,
            TrialRunsLimit = existing.TrialRunsLimit,
            TrialRunsUsed = existing.TrialRunsUsed,
            TrialSeatsLimit = existing.TrialSeatsLimit,
            TrialSeatsUsed = existing.TrialSeatsUsed,
            TrialStatus = TrialLifecycleStatus.Converted,
            TrialSampleRunId = existing.TrialSampleRunId,
        };

        _byId[tenantId] = updated;

        return Task.CompletedTask;
    }

    public Task<TenantWorkspaceLink?> GetFirstWorkspaceAsync(Guid tenantId, CancellationToken ct)
    {
        _ = ct;

        if (!_workspacesByTenant.TryGetValue(tenantId, out List<TenantWorkspaceRow>? list))
            return Task.FromResult<TenantWorkspaceLink?>(null);

        TenantWorkspaceRow? row;

        lock (list)
        {
            row = list.OrderBy(static w => w.CreatedUtc).FirstOrDefault();
        }

        if (row is null)
            return Task.FromResult<TenantWorkspaceLink?>(null);

        return Task.FromResult<TenantWorkspaceLink?>(
            new TenantWorkspaceLink
            {
                WorkspaceId = row.Id,
                DefaultProjectId = row.DefaultProjectId,
            });
    }

    private sealed record TenantWorkspaceRow
    {
        public Guid Id { get; init; }

        public Guid TenantId { get; init; }

        public string Name { get; init; } = string.Empty;

        public Guid DefaultProjectId { get; init; }

        public DateTimeOffset CreatedUtc { get; init; }
    }
}
