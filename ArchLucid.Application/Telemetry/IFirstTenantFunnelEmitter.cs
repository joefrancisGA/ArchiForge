namespace ArchLucid.Application.Telemetry;

/// <summary>
///     Improvement 12 — first-tenant onboarding telemetry funnel.
///     Single application-side ingress for the six funnel events. Implementations decide whether to
///     emit aggregated-only counters or per-tenant rows based on the
///     <c>Telemetry:FirstTenantFunnel:PerTenantEmission</c> feature flag.
/// </summary>
public interface IFirstTenantFunnelEmitter
{
    /// <summary>
    ///     Emit one funnel event for <paramref name="tenantId" />. The tenant id is captured by the
    ///     emitter only when the per-tenant flag is on; aggregated mode never tags the metric or persists
    ///     a row. Never throws on background-telemetry failure (caller treats as fire-and-forget).
    /// </summary>
    /// <param name="eventName">One of <c>FirstTenantFunnelEventNames</c> constants.</param>
    /// <param name="tenantId">Caller-scope tenant id; ignored in aggregated mode.</param>
    /// <param name="ct">Cancellation token (honoured only by the per-tenant SQL write).</param>
    Task EmitAsync(string eventName, Guid tenantId, CancellationToken ct = default);
}
