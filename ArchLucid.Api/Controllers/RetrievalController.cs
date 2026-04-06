using ArchiForge.Api.Auth.Models;
using ArchiForge.Api.ProblemDetails;
using ArchiForge.Core.Scoping;
using ArchiForge.Retrieval.Models;
using ArchiForge.Retrieval.Queries;

using Asp.Versioning;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ArchiForge.Api.Controllers;

/// <summary>
/// HTTP semantic search over retrieval chunks scoped to the caller’s tenant/workspace/project.
/// </summary>
/// <remarks>GET <c>api/retrieval/search</c>; delegates to <see cref="IRetrievalQueryService"/> (same path used indirectly by <c>AskService</c>).</remarks>
[ApiController]
[Authorize(Policy = ArchiForgePolicies.ReadAuthority)]
[ApiVersion("1.0")]
[Route("v{version:apiVersion}/retrieval")]
[EnableRateLimiting("fixed")]
public sealed class RetrievalController(
    IRetrievalQueryService retrievalQueryService,
    IScopeContextProvider scopeProvider)
    : ControllerBase
{
    /// <summary>Runs a vector search for query string <paramref name="q"/>.</summary>
    /// <param name="q">Required natural-language query.</param>
    /// <param name="runId">Optional run filter.</param>
    /// <param name="manifestId">Optional manifest filter.</param>
    /// <param name="topK">Result count (clamped to 1–50; invalid values default to 8).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns><see cref="RetrievalHit"/> list, or 400 when <paramref name="q"/> is missing.</returns>
    [HttpGet("search")]
    [ProducesResponseType(typeof(IReadOnlyList<RetrievalHit>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Search(
        [FromQuery] string q,
        [FromQuery] Guid? runId = null,
        [FromQuery] Guid? manifestId = null,
        [FromQuery] int topK = 8,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(q))
            return this.BadRequestProblem(
                "Query parameter 'q' is required.",
                ProblemTypes.ValidationFailed);

        ScopeContext scope = scopeProvider.GetCurrentScope();

        IReadOnlyList<RetrievalHit> result = await retrievalQueryService.SearchAsync(
            new RetrievalQuery
            {
                TenantId = scope.TenantId,
                WorkspaceId = scope.WorkspaceId,
                ProjectId = scope.ProjectId,
                RunId = runId,
                ManifestId = manifestId,
                QueryText = q.Trim(),
                TopK = topK < 1 ? 8 : Math.Min(topK, 50)
            },
            ct);

        return Ok(result);
    }
}
