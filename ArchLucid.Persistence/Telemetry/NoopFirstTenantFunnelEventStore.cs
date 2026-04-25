namespace ArchLucid.Persistence.Telemetry;

/// <summary>
///     Default <see cref="IFirstTenantFunnelEventStore" /> for aggregated-only deployments — drops every
///     row. Registered as the singleton store unless a host explicitly replaces it (only the SQL host
///     does so, and only when the per-tenant flag is on).
/// </summary>
public sealed class NoopFirstTenantFunnelEventStore : IFirstTenantFunnelEventStore
{
    /// <inheritdoc />
    public Task AppendAsync(string eventName, Guid tenantId, DateTime occurredUtc, CancellationToken ct) =>
        Task.CompletedTask;
}
