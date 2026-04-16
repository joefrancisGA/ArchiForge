namespace ArchLucid.Core.Metering;

/// <summary>Records and queries tenant-scoped usage for billing / FinOps.</summary>
public interface IUsageMeteringService
{
    Task RecordAsync(UsageEvent usageEvent, CancellationToken ct);

    Task RecordBatchAsync(IReadOnlyList<UsageEvent> events, CancellationToken ct);

    Task<IReadOnlyList<TenantUsageSummary>> GetSummaryAsync(
        Guid tenantId,
        DateTimeOffset periodStart,
        DateTimeOffset periodEnd,
        CancellationToken ct);
}
