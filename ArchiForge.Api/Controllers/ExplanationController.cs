using ArchiForge.Api.Auth.Models;
using ArchiForge.AgentRuntime.Explanation;
using ArchiForge.Core.Scoping;
using ArchiForge.Decisioning.Comparison;
using ArchiForge.Persistence.Provenance;
using ArchiForge.Persistence.Queries;
using ArchiForge.Provenance;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ArchiForge.Api.Controllers;

[ApiController]
[Authorize(Policy = ArchiForgePolicies.ReadAuthority)]
[ApiVersion("1.0")]
[Route("api/explain")]
[EnableRateLimiting("fixed")]
public sealed class ExplanationController : ControllerBase
{
    private readonly IAuthorityQueryService _query;
    private readonly IComparisonService _comparison;
    private readonly IExplanationService _explanation;
    private readonly IProvenanceSnapshotRepository _provenanceRepo;
    private readonly IScopeContextProvider _scopeProvider;

    public ExplanationController(
        IAuthorityQueryService query,
        IComparisonService comparison,
        IExplanationService explanation,
        IProvenanceSnapshotRepository provenanceRepo,
        IScopeContextProvider scopeProvider)
    {
        _query = query;
        _comparison = comparison;
        _explanation = explanation;
        _provenanceRepo = provenanceRepo;
        _scopeProvider = scopeProvider;
    }

    [HttpGet("runs/{runId:guid}/explain")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ExplainRun(Guid runId, CancellationToken ct = default)
    {
        var scope = _scopeProvider.GetCurrentScope();
        var detail = await _query.GetRunDetailAsync(scope, runId, ct);
        if (detail?.GoldenManifest is null)
            return NotFound();

        DecisionProvenanceGraph? graph = null;
        var snapshot = await _provenanceRepo.GetByRunIdAsync(scope, runId, ct);
        if (snapshot is not null)
            graph = ProvenanceGraphSerializer.Deserialize(snapshot.GraphJson);

        var result = await _explanation.ExplainRunAsync(detail.GoldenManifest, graph, ct);
        return Ok(result);
    }

    /// <summary>AI narrative for manifest delta between two runs (base → target).</summary>
    [HttpGet("compare/explain")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ExplainComparison(
        [FromQuery] Guid baseRunId,
        [FromQuery] Guid targetRunId,
        CancellationToken ct = default)
    {
        var scope = _scopeProvider.GetCurrentScope();
        var baseRun = await _query.GetRunDetailAsync(scope, baseRunId, ct);
        var targetRun = await _query.GetRunDetailAsync(scope, targetRunId, ct);
        if (baseRun?.GoldenManifest is null || targetRun?.GoldenManifest is null)
            return NotFound();

        var comparison = _comparison.Compare(baseRun.GoldenManifest, targetRun.GoldenManifest);
        var result = await _explanation.ExplainComparisonAsync(comparison, ct);
        return Ok(result);
    }
}
