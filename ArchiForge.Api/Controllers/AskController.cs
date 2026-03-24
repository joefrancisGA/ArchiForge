using ArchiForge.Api.Auth.Models;
using ArchiForge.Api.ProblemDetails;
using ArchiForge.Core.Ask;
using ArchiForge.Core.Scoping;

using Asp.Versioning;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Logging;

namespace ArchiForge.Api.Controllers;

/// <summary>
/// Grounded architect assistant: manifest + provenance + optional comparison + retrieval, with threaded conversations.
/// </summary>
/// <remarks>POST <c>api/ask</c>. Maps validation errors to 400; <see cref="InvalidOperationException"/> from the service to 404.</remarks>
[ApiController]
[Authorize(Policy = ArchiForgePolicies.ReadAuthority)]
[ApiVersion("1.0")]
[Route("api/ask")]
[EnableRateLimiting("fixed")]
public sealed class AskController(
    IAskService ask,
    IScopeContextProvider scopeProvider,
    ILogger<AskController> logger) : ControllerBase
{
    /// <summary>Grounded Q&amp;A over GoldenManifest, provenance graph, optional run comparison, and retrieval hits.</summary>
    /// <param name="request">Thread/run anchors and question (see validation rules in method body).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns><see cref="AskResponse"/> on success.</returns>
    [HttpPost]
    [ProducesResponseType(typeof(AskResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Ask([FromBody] AskRequest? request, CancellationToken ct = default)
    {
        if (request is null)
            return this.BadRequestProblem("Request body is required.", ProblemTypes.RequestBodyRequired);

        if (string.IsNullOrWhiteSpace(request.Question))
            return BadRequest(new
            {
                error = "Question is required."
            });

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
            return BadRequest(new
            {
                error = "Provide both baseRunId and targetRunId for comparison, or omit both."
            });

        try
        {
            var scope = scopeProvider.GetCurrentScope();
            var result = await ask.AskAsync(request, scope, ct);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning(ex, "Ask failed: resource not found.");
            return this.NotFoundProblem(ex.Message, ProblemTypes.ResourceNotFound);
        }
        catch (ArgumentException ex)
        {
            logger.LogWarning(ex, "Ask failed: invalid argument.");
            return this.BadRequestProblem(ex.Message, ProblemTypes.ValidationFailed);
        }
    }
}
