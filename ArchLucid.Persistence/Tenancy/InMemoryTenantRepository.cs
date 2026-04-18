using System.Collections.Concurrent;
using System.Data;

using ArchLucid.Core.Tenancy;

namespace ArchLucid.Persistence.Tenancy;

/// <summary>In-memory tenant registry for tests and <c>InMemory</c> storage mode.</summary>
public sealed class InMemoryTenantRepository : ITenantRepository
{
    private readonly ConcurrentDictionary<Guid, TenantRecord> _byId = new();

    private readonly ConcurrentDictionary<string, Guid> _slugToId = new(StringComparer.OrdinalIgnoreCase);

    private readonly ConcurrentDictionary<Guid, List<TenantWorkspaceRow>> _workspacesByTenant = new();

    private readonly ConcurrentDictionary<Guid, Guid> _entraTenantIdToTenantId = new();

    private readonly ConcurrentDictionary<(Guid TenantId, string PrincipalKey), byte> _trialSeatOccupants = new();

    private readonly object _trialGate = new();

    private readonly ConcurrentDictionary<Guid, byte> _trialFirstManifestCommitted = new();

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
    public Task MarkTrialConvertedAsync(Guid tenantId, TenantTier? newCommercialTier, CancellationToken ct)
    {
        _ = ct;

        if (!_byId.TryGetValue(tenantId, out TenantRecord? existing))
            return Task.CompletedTask;

        if (!string.Equals(existing.TrialStatus, TrialLifecycleStatus.Active, StringComparison.Ordinal))
            return Task.CompletedTask;

        TenantTier tier = newCommercialTier ?? existing.Tier;

        TenantRecord updated = new()
        {
            Id = existing.Id,
            Name = existing.Name,
            Slug = existing.Slug,
            Tier = tier,
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

    /// <inheritdoc />
    public Task TryIncrementActiveTrialRunAsync(
        Guid tenantId,
        CancellationToken ct,
        IDbConnection? connection = null,
        IDbTransaction? transaction = null)
    {
        _ = connection;
        _ = transaction;
        _ = ct;

        lock (_trialGate)
        {
            if (!_byId.TryGetValue(tenantId, out TenantRecord? t))
                return Task.CompletedTask;

            if (!string.Equals(t.TrialStatus, TrialLifecycleStatus.Active, StringComparison.Ordinal) ||
                t.TrialRunsLimit is null)
            {
                return Task.CompletedTask;
            }

            DateTimeOffset now = DateTimeOffset.UtcNow;

            if (t.TrialExpiresUtc is { } exp && exp <= now)
            {
                throw new TrialLimitExceededException(
                    TrialLimitReason.Expired,
                    ComputeDaysRemaining(t.TrialExpiresUtc));
            }

            if (t.TrialRunsUsed >= t.TrialRunsLimit.Value)
            {
                throw new TrialLimitExceededException(
                    TrialLimitReason.RunsExceeded,
                    ComputeDaysRemaining(t.TrialExpiresUtc));
            }

            _byId[tenantId] = CopyTenant(t, trialRunsUsed: t.TrialRunsUsed + 1);
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task TryClaimTrialSeatAsync(Guid tenantId, string principalKey, CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(principalKey);
        _ = ct;

        string key = principalKey.Trim();

        lock (_trialGate)
        {
            if (!_byId.TryGetValue(tenantId, out TenantRecord? t))
                return Task.CompletedTask;

            if (!string.Equals(t.TrialStatus, TrialLifecycleStatus.Active, StringComparison.Ordinal) ||
                t.TrialSeatsLimit is null)
            {
                return Task.CompletedTask;
            }

            if (t.TrialExpiresUtc is { } exp && exp <= DateTimeOffset.UtcNow)
            {
                throw new TrialLimitExceededException(
                    TrialLimitReason.Expired,
                    ComputeDaysRemaining(t.TrialExpiresUtc));
            }

            if (_trialSeatOccupants.ContainsKey((tenantId, key)))
                return Task.CompletedTask;

            if (t.TrialSeatsUsed >= t.TrialSeatsLimit.Value)
            {
                throw new TrialLimitExceededException(
                    TrialLimitReason.SeatsExceeded,
                    ComputeDaysRemaining(t.TrialExpiresUtc));
            }

            _trialSeatOccupants[(tenantId, key)] = 1;

            _byId[tenantId] = CopyTenant(t, trialSeatsUsed: t.TrialSeatsUsed + 1);
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<Guid>> ListTrialLifecycleAutomationTenantIdsAsync(CancellationToken ct)
    {
        _ = ct;

        List<Guid> ids = _byId.Values
            .Where(static t =>
                t.TrialExpiresUtc is not null &&
                !string.IsNullOrWhiteSpace(t.TrialStatus) &&
                !string.Equals(t.TrialStatus, TrialLifecycleStatus.Converted, StringComparison.Ordinal))
            .Select(static t => t.Id)
            .ToList();

        return Task.FromResult<IReadOnlyList<Guid>>(ids);
    }

    /// <inheritdoc />
    public Task<bool> TryRecordTrialLifecycleTransitionAsync(
        Guid tenantId,
        string expectedCurrentStatus,
        string nextStatus,
        string reason,
        CancellationToken ct)
    {
        _ = reason;
        _ = ct;

        lock (_trialGate)
        {
            if (!_byId.TryGetValue(tenantId, out TenantRecord? existing))
            {
                return Task.FromResult(false);
            }

            if (!string.Equals(existing.TrialStatus, expectedCurrentStatus, StringComparison.Ordinal))
            {
                return Task.FromResult(false);
            }

            _byId[tenantId] = CopyTenant(existing, trialStatus: nextStatus);

            return Task.FromResult(true);
        }
    }

    /// <inheritdoc />
    public Task<TrialFirstManifestCommitOutcome?> TryMarkTrialFirstManifestCommittedAsync(
        Guid tenantId,
        DateTimeOffset committedUtc,
        CancellationToken ct)
    {
        _ = ct;

        lock (_trialGate)
        {
            if (!_byId.TryGetValue(tenantId, out TenantRecord? t))
            {
                return Task.FromResult<TrialFirstManifestCommitOutcome?>(null);
            }

            if (t.TrialExpiresUtc is null)
            {
                return Task.FromResult<TrialFirstManifestCommitOutcome?>(null);
            }

            if (!_trialFirstManifestCommitted.TryAdd(tenantId, 0))
            {
                return Task.FromResult<TrialFirstManifestCommitOutcome?>(null);
            }

            DateTimeOffset anchor = t.TrialStartUtc ?? t.CreatedUtc;
            double seconds = (committedUtc - anchor).TotalSeconds;

            double ratio = 0;

            if (t.TrialRunsLimit is { } lim && lim > 0)
            {
                ratio = (double)t.TrialRunsUsed / lim;
            }

            return Task.FromResult<TrialFirstManifestCommitOutcome?>(
                new TrialFirstManifestCommitOutcome
                {
                    SignupToCommitSeconds = seconds,
                    TrialRunUsageRatio = ratio,
                });
        }
    }

    /// <inheritdoc />
    public Task E2eHarnessSetTrialExpiresUtcAsync(Guid tenantId, DateTimeOffset expiresUtc, CancellationToken ct)
    {
        _ = ct;

        lock (_trialGate)
        {
            if (!_byId.TryGetValue(tenantId, out TenantRecord? t))
            {
                return Task.CompletedTask;
            }

            _byId[tenantId] = CopyTenant(t, trialExpiresUtc: expiresUtc);
        }

        return Task.CompletedTask;
    }

    private static TenantRecord CopyTenant(
        TenantRecord source,
        int? trialRunsUsed = null,
        int? trialSeatsUsed = null,
        string? trialStatus = null,
        DateTimeOffset? trialExpiresUtc = null) =>
        new()
        {
            Id = source.Id,
            Name = source.Name,
            Slug = source.Slug,
            Tier = source.Tier,
            EntraTenantId = source.EntraTenantId,
            CreatedUtc = source.CreatedUtc,
            SuspendedUtc = source.SuspendedUtc,
            TrialStartUtc = source.TrialStartUtc,
            TrialExpiresUtc = trialExpiresUtc ?? source.TrialExpiresUtc,
            TrialRunsLimit = source.TrialRunsLimit,
            TrialRunsUsed = trialRunsUsed ?? source.TrialRunsUsed,
            TrialSeatsLimit = source.TrialSeatsLimit,
            TrialSeatsUsed = trialSeatsUsed ?? source.TrialSeatsUsed,
            TrialStatus = trialStatus ?? source.TrialStatus,
            TrialSampleRunId = source.TrialSampleRunId,
        };

    private static int ComputeDaysRemaining(DateTimeOffset? trialExpiresUtc)
    {
        if (trialExpiresUtc is null)
            return 0;

        double totalDays = (trialExpiresUtc.Value - DateTimeOffset.UtcNow).TotalDays;
        int days = (int)Math.Floor(totalDays);

        return days < 0 ? 0 : days;
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
