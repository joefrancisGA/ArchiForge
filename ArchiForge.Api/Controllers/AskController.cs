using ArchiForge.Api.Auth.Models;
using ArchiForge.Core.Ask;
using ArchiForge.Core.Scoping;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ArchiForge.Api.Controllers;

[ApiController]
[Authorize(Policy = ArchiForgePolicies.ReadAuthority)]
[ApiVersion("1.0")]
[Route("api/ask")]
[EnableRateLimiting("fixed")]
public sealed class AskController : ControllerBase
{
    private readonly IAskService _ask;
    private readonly IScopeContextProvider _scopeProvider;

    public AskController(IAskService ask, IScopeContextProvider scopeProvider)
    {
        _ask = ask;
        _scopeProvider = scopeProvider;
    }

    /// <summary>Grounded Q&amp;A over GoldenManifest, provenance graph, and optional run comparison.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(AskResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Ask([FromBody] AskRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Question))
            return BadRequest(new { error = "Question is required." });

        if (request.RunId is null && request.ThreadId is null)
        {
            return BadRequest(new
            {
                error = "Provide runId (new conversation) or threadId (continue an existing conversation)."
            });
        }

        var hasBase = request.BaseRunId.HasValue;
        var hasTarget = request.TargetRunId.HasValue;
        if (hasBase != hasTarget)
            return BadRequest(new { error = "Provide both baseRunId and targetRunId for comparison, or omit both." });

        try
        {
            var scope = _scopeProvider.GetCurrentScope();
            var result = await _ask.AskAsync(request, scope, ct);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}
