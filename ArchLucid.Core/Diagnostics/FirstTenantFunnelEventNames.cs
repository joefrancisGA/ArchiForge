namespace ArchLucid.Core.Diagnostics;

/// <summary>
///     Canonical six-event catalog for the first-tenant onboarding telemetry funnel
///     (Improvement 12 / pending question 40). The catalog is the contract between:
///     <list type="bullet">
///         <item>the operator-shell client (<c>archlucid-ui/src/lib/first-tenant-funnel-telemetry.ts</c>),</item>
///         <item>the API ingest (<c>POST /v1/diagnostics/first-tenant-funnel</c>),</item>
///         <item>the application emitter (<c>FirstTenantFunnelEmitter</c>),</item>
///         <item>and the SQL row schema (<c>dbo.FirstTenantFunnelEvents.EventName</c>).</item>
///     </list>
///     Adding or renaming an event requires updating all four surfaces and the privacy notice §3.A.
/// </summary>
public static class FirstTenantFunnelEventNames
{
    /// <summary>New tenant signup persisted (post <c>POST /v1/register</c> 2xx).</summary>
    public const string Signup = "signup";

    /// <summary>Operator clicked "Show me around" — opt-in tour launcher (Q9).</summary>
    public const string TourOptIn = "tour_opt_in";

    /// <summary>First architecture run created via the new-run wizard (post <c>POST /v1/runs</c> 2xx).</summary>
    public const string FirstRunStarted = "first_run_started";

    /// <summary>First architecture run committed to a golden manifest (post commit-run success).</summary>
    public const string FirstRunCommitted = "first_run_committed";

    /// <summary>First finding viewed on the run-detail or finding-detail page.</summary>
    public const string FirstFindingViewed = "first_finding_viewed";

    /// <summary>
    ///     Server-side derived: all five preceding events fell within 30 minutes of <see cref="Signup" />.
    ///     Emitted at most once per tenant.
    /// </summary>
    public const string ThirtyMinuteMilestone = "thirty_minute_milestone";

    /// <summary>Frozen ordered set used for validation and dashboard tile names.</summary>
    public static readonly IReadOnlyList<string> All =
    [
        Signup,
        TourOptIn,
        FirstRunStarted,
        FirstRunCommitted,
        FirstFindingViewed,
        ThirtyMinuteMilestone
    ];

    /// <summary>True when <paramref name="value" /> is one of the six canonical event names.</summary>
    public static bool IsValid(string? value) =>
        !string.IsNullOrWhiteSpace(value) && All.Contains(value, StringComparer.Ordinal);
}
