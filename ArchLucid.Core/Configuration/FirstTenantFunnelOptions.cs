namespace ArchLucid.Core.Configuration;

/// <summary>
///     Improvement 12 — first-tenant onboarding telemetry funnel. Default emission shape is
///     <b>aggregated-only</b>; per-tenant correlation is gated by an owner-only feature flag
///     because it changes the privacy-notice surface (see
///     <c>docs/security/PRIVACY_NOTE.md</c> §3.A and pending question 40).
/// </summary>
public sealed class FirstTenantFunnelOptions
{
    /// <summary>Configuration section name. Mirrors the prompt: <c>Telemetry:FirstTenantFunnel</c>.</summary>
    public const string SectionName = "Telemetry:FirstTenantFunnel";

    /// <summary>
    ///     When <c>true</c>, the funnel emitter also tags Application Insights metrics with <c>tenant_id</c>
    ///     and persists per-tenant rows to <c>dbo.FirstTenantFunnelEvents</c>. <b>Default is <c>false</c></b>
    ///     per pending question 40 — owner-only flip after the privacy review records the activity in
    ///     <c>docs/security/PRIVACY_NOTE.md</c> §3.A.
    /// </summary>
    public bool PerTenantEmission
    {
        get;
        set;
    }
}
