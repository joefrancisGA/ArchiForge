using ArchiForge.Api.Auth.Models;
using ArchiForge.Api.ProblemDetails;
using ArchiForge.Core.Scoping;
using ArchiForge.Persistence.Provenance;
using ArchiForge.Provenance;

using Asp.Versioning;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ArchiForge.Api.Controllers;

[ApiController]
[Authorize(Policy = ArchiForgePolicies.ReadAuthority)]
[ApiVersion("1.0")]
[Route("api/authority")]
[EnableRateLimiting("fixed")]
public sealed class ProvenanceQueryController(
    IProvenanceSnapshotRepository repo,
    IProvenanceQueryService graphQuery,
    IScopeContextProvider scopeProvider)
    : ControllerBase
{
    [HttpGet("runs/{runId:guid}/provenance")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProvenance(Guid runId, CancellationToken ct = default)
    {
        ScopeContext scope = scopeProvider.GetCurrentScope();
        DecisionProvenanceSnapshot? snapshot = await repo.GetByRunIdAsync(scope, runId, ct);
        if (snapshot is null)
            return this.NotFoundProblem($"Provenance snapshot for run '{runId}' was not found.", ProblemTypes.ResourceNotFound);

        return Ok(snapshot);
    }

    [HttpGet("runs/{runId:guid}/graph")]
    [ProducesResponseType(typeof(GraphViewModel), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetFullGraph(Guid runId, CancellationToken ct = default)
    {
        ScopeContext scope = scopeProvider.GetCurrentScope();
        GraphViewModel? vm = await graphQuery.GetFullGraphAsync(scope, runId, ct);
        return vm is null ? NotFound() : Ok(vm);
    }

    /// <summary>Returns the provenance subgraph rooted at the specified decision node.</summary>
    /// <param name="runId">Run that owns the graph.</param>
    /// <param name="decisionKey">Provenance decision node id (GUID) or architecture decision reference id.</param>
    /// <param name="ct">Cancellation token.</param>
    [HttpGet("runs/{runId:guid}/graph/decision/{decisionKey}")]
    [ProducesResponseType(typeof(GraphViewModel), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetDecisionGraph(
        Guid runId,
        string decisionKey,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(decisionKey))
            return this.BadRequestProblem("decisionKey is required.", ProblemTypes.BadRequest);

        ScopeContext scope = scopeProvider.GetCurrentScope();
        GraphViewModel? vm = await graphQuery.GetDecisionSubgraphAsync(scope, runId, decisionKey, ct);
        return vm is null ? NotFound() : Ok(vm);
    }

    [HttpGet("runs/{runId:guid}/graph/node/{nodeId:guid}")]
    [ProducesResponseType(typeof(GraphViewModel), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetNodeNeighborhood(
        Guid runId,
        Guid nodeId,
        [FromQuery] int depth = 1,
        CancellationToken ct = default)
    {
        int safeDepth = Math.Clamp(depth, 1, 10);
        ScopeContext scope = scopeProvider.GetCurrentScope();
        GraphViewModel? vm = await graphQuery.GetNodeNeighborhoodAsync(scope, runId, nodeId, safeDepth, ct);
        return vm is null ? NotFound() : Ok(vm);
    }
}
