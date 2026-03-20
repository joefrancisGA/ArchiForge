using ArchiForge.Api.Auth.Models;
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
public sealed class ProvenanceQueryController : ControllerBase
{
    private readonly IProvenanceSnapshotRepository _repo;
    private readonly IProvenanceQueryService _graphQuery;
    private readonly IScopeContextProvider _scopeProvider;

    public ProvenanceQueryController(
        IProvenanceSnapshotRepository repo,
        IProvenanceQueryService graphQuery,
        IScopeContextProvider scopeProvider)
    {
        _repo = repo;
        _graphQuery = graphQuery;
        _scopeProvider = scopeProvider;
    }

    [HttpGet("runs/{runId:guid}/provenance")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProvenance(Guid runId, CancellationToken ct = default)
    {
        var scope = _scopeProvider.GetCurrentScope();
        var snapshot = await _repo.GetByRunIdAsync(scope, runId, ct);
        if (snapshot is null)
            return NotFound();

        return Ok(snapshot);
    }

    [HttpGet("runs/{runId:guid}/graph")]
    [ProducesResponseType(typeof(GraphViewModel), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetFullGraph(Guid runId, CancellationToken ct = default)
    {
        var scope = _scopeProvider.GetCurrentScope();
        var vm = await _graphQuery.GetFullGraphAsync(scope, runId, ct);
        return vm is null ? NotFound() : Ok(vm);
    }

    /// <param name="decisionKey">Provenance decision node id (GUID) or architecture decision reference id.</param>
    [HttpGet("runs/{runId:guid}/graph/decision/{decisionKey}")]
    [ProducesResponseType(typeof(GraphViewModel), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetDecisionGraph(
        Guid runId,
        string decisionKey,
        CancellationToken ct = default)
    {
        var scope = _scopeProvider.GetCurrentScope();
        var vm = await _graphQuery.GetDecisionSubgraphAsync(scope, runId, decisionKey, ct);
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
        var scope = _scopeProvider.GetCurrentScope();
        var vm = await _graphQuery.GetNodeNeighborhoodAsync(scope, runId, nodeId, depth, ct);
        return vm is null ? NotFound() : Ok(vm);
    }
}
