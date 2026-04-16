using System.Collections.Concurrent;

using ArchLucid.Core.Metering;
using ArchLucid.Persistence.Interfaces;

namespace ArchLucid.Persistence.Tenancy;

/// <summary>In-memory usage events for tests and <c>InMemory</c> storage mode.</summary>
public sealed class InMemoryUsageEventRepository : IUsageEventRepository
{
    private readonly ConcurrentBag<UsageEvent> _events = new();

    public Task InsertAsync(UsageEvent usageEvent, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(usageEvent);
        _ = ct;

        _events.Add(usageEvent);

        return Task.CompletedTask;
    }

    public Task InsertBatchAsync(IReadOnlyList<UsageEvent> events, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(events);
        _ = ct;

        foreach (UsageEvent e in events)
            _events.Add(e);

        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<TenantUsageSummary>> AggregateByKindAsync(
        Guid tenantId,
        DateTimeOffset periodStart,
        DateTimeOffset periodEnd,
        CancellationToken ct)
    {
        _ = ct;

        List<TenantUsageSummary> summaries = _events
            .Where(e => e.TenantId == tenantId && e.RecordedUtc >= periodStart && e.RecordedUtc < periodEnd)
            .GroupBy(e => e.Kind)
            .Select(
                g => new TenantUsageSummary
                {
                    TenantId = tenantId,
                    Kind = g.Key,
                    TotalQuantity = g.Sum(static x => x.Quantity),
                    PeriodStartUtc = periodStart,
                    PeriodEndUtc = periodEnd,
                })
            .ToList();

        return Task.FromResult<IReadOnlyList<TenantUsageSummary>>(summaries);
    }

    public Task<IReadOnlyList<UsageEvent>> ListAsync(
        Guid tenantId,
        DateTimeOffset periodStart,
        DateTimeOffset periodEnd,
        UsageMeterKind? kindFilter,
        int take,
        CancellationToken ct)
    {
        _ = ct;

        IEnumerable<UsageEvent> q = _events.Where(
            e => e.TenantId == tenantId && e.RecordedUtc >= periodStart && e.RecordedUtc < periodEnd);

        if (kindFilter.HasValue)
            q = q.Where(e => e.Kind == kindFilter.Value);

        IReadOnlyList<UsageEvent> list = q.OrderByDescending(static e => e.RecordedUtc).Take(Math.Max(1, take)).ToList();

        return Task.FromResult(list);
    }
}
