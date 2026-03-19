using ArchiForge.Api.Models;
using ArchiForge.Api.Services;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
 
namespace ArchiForge.Api.Controllers;
 
[ApiController]
[Authorize(AuthenticationSchemes = "ApiKey")]
[ApiVersion("1.0")]
[Route("v{version:apiVersion}/architecture")]
[EnableRateLimiting("fixed")]
public sealed class DiagnosticsController(IReplayDiagnosticsRecorder replayDiagnosticsRecorder) : ControllerBase
{
    [HttpGet("comparisons/diagnostics/replay")]
    [Authorize(Policy = "CanViewReplayDiagnostics")]
    [ProducesResponseType(typeof(ReplayDiagnosticsResponse), StatusCodes.Status200OK)]
    public IActionResult GetReplayDiagnostics([FromQuery] int maxCount = 50)
    {
        var entries = replayDiagnosticsRecorder.GetRecent(Math.Clamp(maxCount, 1, 100));
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

