using ArchLucid.Api.Attributes;
using ArchLucid.Api.Models.Diagnostics;
using ArchLucid.Core.Authorization;
using ArchLucid.Core.Diagnostics;
using ArchLucid.Core.Tenancy;

using Asp.Versioning;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ArchLucid.Api.Controllers.Diagnostics;

/// <summary>Read-only operator diagnostics for onboarding funnel counters.</summary>
[ApiController]
[Authorize(Policy = ArchLucidPolicies.ReadAuthority)]
[ApiVersion("1.0")]
[Route("v{version:apiVersion}/diagnostics")]
[EnableRateLimiting("fixed")]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
[RequiresCommercialTenantTier(TenantTier.Standard)]
public sealed class OperatorTaskSuccessDiagnosticsController(
    IInstrumentationCounterSnapshotProvider counterSnapshotProvider) : ControllerBase
{
    private readonly IInstrumentationCounterSnapshotProvider _counterSnapshotProvider =
        counterSnapshotProvider ?? throw new ArgumentNullException(nameof(counterSnapshotProvider));

    /// <summary>Returns cumulative <c>archlucid_operator_task_success_total</c> by task (process lifetime).</summary>
    [HttpGet("operator-task-success-rates")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(OperatorTaskSuccessRatesResponse), StatusCodes.Status200OK)]
    public IActionResult GetOperatorTaskSuccessRates()
    {
        InstrumentationCounterSnapshot snap = _counterSnapshotProvider.GetSnapshot();

        long firstRun = snap.OperatorTaskSuccessByTask.TryGetValue("first_run_committed", out long fr) ? fr : 0;
        long firstSession = snap.OperatorTaskSuccessByTask.TryGetValue("first_session_completed", out long fs) ? fs : 0;

        double ratio = firstSession <= 0 ? 0d : (double)firstRun / firstSession;

        OperatorTaskSuccessRatesResponse body = new()
        {
            WindowNote =
                "Counts are process-lifetime totals from the in-process meter listener (reset when the API host restarts). "
                + "They approximate funnel conversion for the current deployment; rolling 7-day rates require a time-series store.",
            FirstRunCommittedTotal = firstRun,
            FirstSessionCompletedTotal = firstSession,
            FirstRunCommittedPerSessionRatio = ratio
        };

        return Ok(body);
    }
}
