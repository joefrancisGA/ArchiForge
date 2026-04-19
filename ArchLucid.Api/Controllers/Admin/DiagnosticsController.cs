using ArchLucid.Core.Authorization;
using ArchLucid.Api.Models;
using ArchLucid.Host.Core.Services;

using Asp.Versioning;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ArchLucid.Api.Controllers.Admin;

/// <summary>
/// Exposes internal diagnostics for replay and comparison operations; restricted to privileged principals.
/// </summary>
[ApiController]
[Authorize(Policy = ArchLucidPolicies.ReadAuthority)]
[ApiVersion("1.0")]
[Route("v{version:apiVersion}/architecture")]
[EnableRateLimiting("fixed")]
public sealed class DiagnosticsController(IReplayDiagnosticsRecorder replayDiagnosticsRecorder) : ControllerBase
{
    private const int MaxReplayDiagnosticsEntries = 100;

    /// <summary>Returns the most recent in-memory replay diagnostic entries captured by <see cref="IReplayDiagnosticsRecorder"/>.</summary>
    /// <param name="maxCount">Maximum number of entries to return (1–<see cref="MaxReplayDiagnosticsEntries"/>; defaults to 50).</param>
    /// <returns>200 with a <see cref="ReplayDiagnosticsResponse"/> containing up to <paramref name="maxCount"/> entries.</returns>
    [HttpGet("comparisons/diagnostics/replay")]
    [Authorize(Policy = ArchLucidPolicies.CanViewReplayDiagnostics)]
    [ProducesResponseType(typeof(ReplayDiagnosticsResponse), StatusCodes.Status200OK)]
    public IActionResult GetReplayDiagnostics([FromQuery] int maxCount = 50)
    {
        IReadOnlyList<ReplayDiagnosticsEntry> entries = replayDiagnosticsRecorder.GetRecent(
            Math.Clamp(maxCount, 1, MaxReplayDiagnosticsEntries));
        return Ok(new ReplayDiagnosticsResponse
        {
            RecentReplays = entries.Select(e => new ReplayDiagnosticsEntryDto
            {
                TimestampUtc = e.TimestampUtc,
                ComparisonRecordId = e.ComparisonRecordId,
                ComparisonType = e.ComparisonType,
                Format = e.Format,
                ReplayMode = e.ReplayMode,
                PersistReplay = e.PersistReplay,
                DurationMs = e.DurationMs,
                Success = e.Success,
                VerificationPassed = e.VerificationPassed,
                PersistedReplayRecordId = e.PersistedReplayRecordId,
                ErrorMessage = e.ErrorMessage,
                MetadataOnly = e.MetadataOnly
            }).ToList()
        });
    }
}

