using ArchiForge.Api.Auth.Models;
using ArchiForge.Api.ProblemDetails;
using ArchiForge.Core.Comparison;
using ArchiForge.Core.Scoping;
using ArchiForge.Decisioning.Comparison;
using ArchiForge.Decisioning.Models;
using ArchiForge.Persistence.Queries;

using Asp.Versioning;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ArchiForge.Api.Controllers;

/// <summary>
/// HTTP API for structured golden-manifest comparison between two runs in the caller’s scope (base → target).
/// </summary>
/// <remarks>
/// Uses <see cref="IAuthorityQueryService.GetRunDetailAsync"/> for both runs, then <see cref="IComparisonService.Compare"/>. For flat diff lists, see <c>api/authority/compare</c>.
/// </remarks>
[ApiController]
[Authorize(Policy = ArchiForgePolicies.ReadAuthority)]
[ApiVersion("1.0")]
[Route("api/compare")]
[EnableRateLimiting("fixed")]
public sealed class ComparisonController(
    IAuthorityQueryService query,
    IComparisonService comparison,
    IScopeContextProvider scopeProvider)
    : ControllerBase
{
    /// <summary>Structured <see cref="GoldenManifest"/> delta between two runs (base → target).</summary>
    /// <param name="baseRunId">Earlier or baseline run.</param>
    /// <param name="targetRunId">Later or candidate run.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns><see cref="ComparisonResult"/> when both runs exist in scope and each has a golden manifest; otherwise 404.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(ComparisonResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CompareRuns(
        [FromQuery] Guid baseRunId,
        [FromQuery] Guid targetRunId,
        CancellationToken ct = default)
    {
        ScopeContext scope = scopeProvider.GetCurrentScope();
        RunDetailDto? baseRun = await query.GetRunDetailAsync(scope, baseRunId, ct);
        RunDetailDto? targetRun = await query.GetRunDetailAsync(scope, targetRunId, ct);

        if (baseRun is null)
            return this.NotFoundProblem($"Run '{baseRunId}' was not found.", ProblemTypes.RunNotFound);

        if (targetRun is null)
            return this.NotFoundProblem($"Run '{targetRunId}' was not found.", ProblemTypes.RunNotFound);

        if (baseRun.GoldenManifest is null)
            return this.NotFoundProblem($"Run '{baseRunId}' does not have a committed golden manifest.", ProblemTypes.ManifestNotFound);

        if (targetRun.GoldenManifest is null)
            return this.NotFoundProblem($"Run '{targetRunId}' does not have a committed golden manifest.", ProblemTypes.ManifestNotFound);

        ComparisonResult result = comparison.Compare(baseRun.GoldenManifest, targetRun.GoldenManifest);
        return Ok(result);
    }
}
