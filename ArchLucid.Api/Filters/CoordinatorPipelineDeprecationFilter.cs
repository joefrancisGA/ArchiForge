using Microsoft.AspNetCore.Mvc.Filters;

namespace ArchLucid.Api.Filters;

/// <summary>
/// Emits the standards-track deprecation signal (RFC 9745 <c>Deprecation</c>, RFC 8594 <c>Sunset</c>,
/// RFC 8288 <c>Link</c>) on every response from a coordinator-pipeline action that ADR 0021 is retiring.
/// </summary>
/// <remarks>
/// <para>
/// The values here are deliberately <b>hard-coded constants</b> rather than configuration: ADR 0021 Phase 2
/// is an architectural deprecation milestone, not a per-deployment toggle. The Sunset date below is the
/// <em>earliest</em> possible removal date for the coordinator interface family per the ADR's Phase 3
/// exit-gate calendar (Phase 1 ships → 30 days at full traffic → Phase 2 deprecation window → Phase 3
/// retirement). Updating the date requires editing this file (PR-reviewed) and amending ADR 0021.
/// </para>
/// <para>
/// The headers are written via <see cref="Microsoft.AspNetCore.Http.HttpResponse.OnStarting(System.Func{System.Threading.Tasks.Task})"/>
/// so they appear on success responses, problem-details responses, and exception-mapped responses alike —
/// per RFC 9745 §3 a deprecated resource MUST advertise its status on every applicable response.
/// </para>
/// </remarks>
public sealed class CoordinatorPipelineDeprecationFilter : IAsyncActionFilter
{
    /// <summary>
    /// Per RFC 8594 §3 the earliest date the resource may be removed. Set to <b>2026-07-20</b> — one full
    /// quarter from the ADR 0021 Phase 2 ship date (2026-04-21), giving published clients a reasonable
    /// window to migrate to the unified Authority routes once ADR 0021 Phase 3 lands.
    /// </summary>
    public const string SunsetHttpDate = "Mon, 20 Jul 2026 00:00:00 GMT";

    /// <summary>
    /// RFC 8288 link header pointing at ADR 0021 (the canonical migration target). The link is rendered
    /// as Markdown so consumers can follow it without a browser round-trip.
    /// </summary>
    public const string DeprecationLink =
        "<https://github.com/joefrancisGA/ArchLucid/blob/main/docs/adr/0021-coordinator-pipeline-strangler-plan.md>; rel=\"deprecation\"; type=\"text/markdown\"";

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (context is null) throw new ArgumentNullException(nameof(context));
        if (next is null) throw new ArgumentNullException(nameof(next));

        AttachHeadersOnStarting(context.HttpContext);

        await next();
    }

    /// <summary>
    /// Idempotently sets the three deprecation headers immediately before the response starts. Uses
    /// indexer-assignment (rather than <c>Append</c>) so an enabled global ApiDeprecationHeadersMiddleware
    /// cannot duplicate the signal — the route-scoped value wins.
    /// </summary>
    private static void AttachHeadersOnStarting(HttpContext httpContext)
    {
        if (httpContext is null) throw new ArgumentNullException(nameof(httpContext));

        httpContext.Response.OnStarting(() =>
        {
            httpContext.Response.Headers["Deprecation"] = "true";
            httpContext.Response.Headers["Sunset"] = SunsetHttpDate;
            httpContext.Response.Headers["Link"] = DeprecationLink;

            return Task.CompletedTask;
        });
    }
}
