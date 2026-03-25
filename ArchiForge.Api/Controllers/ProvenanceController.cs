using ArchiForge.Api.Auth.Models;
using ArchiForge.Core.Scoping;
using ArchiForge.Provenance;

using Asp.Versioning;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ArchiForge.Api.Controllers;

/// <summary>Provenance graph queries for UI (alias of authority graph endpoints under <c>/api/provenance</c>).</summary>
[ApiController]
[Authorize(Policy = ArchiForgePolicies.ReadAuthority)]
[ApiVersion("1.0")]
[Route("api/provenance")]
[EnableRateLimiting("fixed")]
public sealed class ProvenanceController(
    IProvenanceQueryService service,
    IScopeContextProvider scopeProvider)
    : ControllerBase
{
    [HttpGet("runs/{runId:guid}/graph")]
    [ProducesResponseType(typeof(GraphViewModel), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetFullGraph(Guid runId, CancellationToken ct = default)
    {
        ScopeContext scope = scopeProvider.GetCurrentScope();
        GraphViewModel? vm = await service.GetFullGraphAsync(scope, runId, ct);
        return vm is null ? NotFound() : Ok(vm);
    }

    /// <param name="runId"></param>
    /// <param name="decisionKey">Decision provenance node id (GUID) or architecture decision reference id on the node.</param>
    /// <param name="ct"></param>
    [HttpGet("runs/{runId:guid}/graph/decision/{decisionKey}")]
    [ProducesResponseType(typeof(GraphViewModel), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetDecisionGraph(
        Guid runId,
        string decisionKey,
        CancellationToken ct = default)
    {
        ScopeContext scope = scopeProvider.GetCurrentScope();
        GraphViewModel? vm = await service.GetDecisionSubgraphAsync(scope, runId, decisionKey, ct);
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
        depth = Math.Clamp(depth, 1, 5);
        ScopeContext scope = scopeProvider.GetCurrentScope();
        GraphViewModel? vm = await service.GetNodeNeighborhoodAsync(scope, runId, nodeId, depth, ct);
        return vm is null ? NotFound() : Ok(vm);
    }
}
