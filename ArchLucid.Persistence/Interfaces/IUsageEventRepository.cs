using ArchLucid.Core.Metering;

namespace ArchLucid.Persistence.Interfaces;

/// <summary>Persistence for <c>dbo.UsageEvents</c>.</summary>
public interface IUsageEventRepository
{
    Task InsertAsync(UsageEvent usageEvent, CancellationToken ct);

    Task InsertBatchAsync(IReadOnlyList<UsageEvent> events, CancellationToken ct);

    Task<IReadOnlyList<TenantUsageSummary>> AggregateByKindAsync(
        Guid tenantId,
        DateTimeOffset periodStart,
        DateTimeOffset periodEnd,
        CancellationToken ct);

    Task<IReadOnlyList<UsageEvent>> ListAsync(
        Guid tenantId,
        DateTimeOffset periodStart,
        DateTimeOffset periodEnd,
        UsageMeterKind? kindFilter,
        int take,
        CancellationToken ct);
}
