namespace ArchLucid.Core.Metering;

/// <summary>No-op metering when <c>Metering:Enabled</c> is false.</summary>
public sealed class NullUsageMeteringService : IUsageMeteringService
{
    public Task RecordAsync(UsageEvent usageEvent, CancellationToken ct)
    {
        return Task.CompletedTask;
    }

    public Task RecordBatchAsync(IReadOnlyList<UsageEvent> events, CancellationToken ct)
    {
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<TenantUsageSummary>> GetSummaryAsync(
        Guid tenantId,
        DateTimeOffset periodStart,
        DateTimeOffset periodEnd,
        CancellationToken ct)
    {
        return Task.FromResult<IReadOnlyList<TenantUsageSummary>>([]);
    }
}
