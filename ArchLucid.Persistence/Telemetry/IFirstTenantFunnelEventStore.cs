namespace ArchLucid.Persistence.Telemetry;

/// <summary>
///     Persistence seam for per-tenant funnel rows (Improvement 12). Used by the application emitter
///     only when the <c>Telemetry:FirstTenantFunnel:PerTenantEmission</c> feature flag is on; otherwise
///     the emitter never calls into a store. The default registration is a no-op store so a
///     misconfigured DI container in aggregated-only deployments cannot accidentally write rows.
/// </summary>
public interface IFirstTenantFunnelEventStore
{
    /// <summary>
    ///     Append one funnel row. Implementations MUST NOT capture <c>userId</c>, IP address, or any
    ///     personal data beyond <paramref name="tenantId" />.
    /// </summary>
    Task AppendAsync(string eventName, Guid tenantId, DateTime occurredUtc, CancellationToken ct);
}
