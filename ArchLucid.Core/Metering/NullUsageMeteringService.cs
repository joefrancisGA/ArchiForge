namespace ArchLucid.Core.Metering;

/// <summary>No-op metering when <c>Metering:Enabled</c> is false.</summary>
public sealed class NullUsageMeteringService : IUsageMeteringService
{
    public Task RecordAsync(UsageEvent usageEvent, CancellationToken ct) => Task.CompletedTask;

    public Task RecordBatchAsync(IReadOnlyList<UsageEvent> events, CancellationToken ct) => Task.CompletedTask;

    public Task<IReadOnlyList<TenantUsageSummary>> GetSummaryAsync(
        Guid tenantId,
        DateTimeOffset periodStart,
        DateTimeOffset periodEnd,
        CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<TenantUsageSummary>>(Array.Empty<TenantUsageSummary>());
}
