using ArchiForge.Api.Auth.Models;
using ArchiForge.Core.Scoping;
using ArchiForge.Retrieval.Models;
using ArchiForge.Retrieval.Queries;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ArchiForge.Api.Controllers;

[ApiController]
[Authorize(Policy = ArchiForgePolicies.ReadAuthority)]
[ApiVersion("1.0")]
[Route("api/retrieval")]
[EnableRateLimiting("fixed")]
public sealed class RetrievalController : ControllerBase
{
    private readonly IRetrievalQueryService _retrievalQueryService;
    private readonly IScopeContextProvider _scopeProvider;

    public RetrievalController(
        IRetrievalQueryService retrievalQueryService,
        IScopeContextProvider scopeProvider)
    {
        _retrievalQueryService = retrievalQueryService;
        _scopeProvider = scopeProvider;
    }

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
            return BadRequest(new { error = "Query parameter 'q' is required." });

        var scope = _scopeProvider.GetCurrentScope();

        var result = await _retrievalQueryService.SearchAsync(
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
