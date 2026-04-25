namespace ArchLucid.Api.Models;

/// <summary>
///     Body for <c>POST /v1/diagnostics/first-tenant-funnel</c> (Improvement 12). The client never
///     supplies <c>tenantId</c> — the server infers it from the request scope (<c>IScopeContextProvider</c>)
///     so the UI surface cannot accidentally leak the wrong tenant.
/// </summary>
public sealed class FirstTenantFunnelEventRequest
{
    /// <summary>
    ///     Funnel event name; one of <c>FirstTenantFunnelEventNames</c> constants
    ///     (<c>signup</c>, <c>tour_opt_in</c>, <c>first_run_started</c>, <c>first_run_committed</c>,
    ///     <c>first_finding_viewed</c>, <c>thirty_minute_milestone</c>).
    /// </summary>
    public string? Event
    {
        get;
        init;
    }
}
