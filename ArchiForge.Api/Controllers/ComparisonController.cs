using ArchiForge.Api.Auth.Models;
using ArchiForge.Core.Comparison;
using ArchiForge.Core.Scoping;
using ArchiForge.Decisioning.Comparison;
using ArchiForge.Persistence.Queries;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ArchiForge.Api.Controllers;

[ApiController]
[Authorize(Policy = ArchiForgePolicies.ReadAuthority)]
[ApiVersion("1.0")]
[Route("api/compare")]
[EnableRateLimiting("fixed")]
public sealed class ComparisonController : ControllerBase
{
    private readonly IAuthorityQueryService _query;
    private readonly IComparisonService _comparison;
    private readonly IScopeContextProvider _scopeProvider;

    public ComparisonController(
        IAuthorityQueryService query,
        IComparisonService comparison,
        IScopeContextProvider scopeProvider)
    {
        _query = query;
        _comparison = comparison;
        _scopeProvider = scopeProvider;
    }

    /// <summary>Structured GoldenManifest delta between two runs (base → target).</summary>
    [HttpGet]
    [ProducesResponseType(typeof(ComparisonResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CompareRuns(
        [FromQuery] Guid baseRunId,
        [FromQuery] Guid targetRunId,
        CancellationToken ct = default)
    {
        var scope = _scopeProvider.GetCurrentScope();
        var baseRun = await _query.GetRunDetailAsync(scope, baseRunId, ct);
        var targetRun = await _query.GetRunDetailAsync(scope, targetRunId, ct);

        if (baseRun?.GoldenManifest is null || targetRun?.GoldenManifest is null)
            return NotFound();

        var result = _comparison.Compare(baseRun.GoldenManifest, targetRun.GoldenManifest);
        return Ok(result);
    }
}
