using ArchiForge.Api.Auth.Models;
using ArchiForge.Api.ProblemDetails;
using ArchiForge.Core.Scoping;
using ArchiForge.Provenance;

using Asp.Versioning;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

using MvcProblemDetails = Microsoft.AspNetCore.Mvc.ProblemDetails;

namespace ArchiForge.Api.Controllers;

/// <summary>
/// Provenance graph queries for UI (alias of authority graph endpoints under <c>/api/provenance</c>).
/// </summary>
/// <remarks>
/// All three actions return a <see cref="GraphViewModel"/> for the run scoped to the caller's tenant/workspace/project.
/// Returns 404 with Problem Details when the run or graph snapshot is missing.
/// </remarks>
[ApiController]
[Authorize(Policy = ArchiForgePolicies.ReadAuthority)]
[ApiVersion("1.0")]
[Route("v{version:apiVersion}/provenance")]
[EnableRateLimiting("fixed")]
public sealed class ProvenanceController(
    IProvenanceQueryService service,
    IScopeContextProvider scopeProvider)
    : ControllerBase
{
    /// <summary>Returns the full provenance graph for a run.</summary>
    /// <param name="runId">Architecture run id.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The full <see cref="GraphViewModel"/>, or 404 when the run or snapshot is missing.</returns>
    [HttpGet("runs/{runId:guid}/graph")]
    [ProducesResponseType(typeof(GraphViewModel), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(MvcProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetFullGraph(Guid runId, CancellationToken ct = default)
    {
        ScopeContext scope = scopeProvider.GetCurrentScope();
        GraphViewModel? vm = await service.GetFullGraphAsync(scope, runId, ct);
        return vm is null
            ? this.NotFoundProblem($"Provenance graph for run '{runId}' was not found.", ProblemTypes.ResourceNotFound)
            : Ok(vm);
    }

    /// <summary>Returns the provenance sub-graph rooted at a specific decision node.</summary>
    /// <param name="runId">Architecture run id.</param>
    /// <param name="decisionKey">Decision provenance node id (GUID) or architecture decision reference id on the node.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Sub-graph rooted at the decision node, or 404 when not found.</returns>
    [HttpGet("runs/{runId:guid}/graph/decision/{decisionKey}")]
    [ProducesResponseType(typeof(GraphViewModel), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(MvcProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetDecisionGraph(
        Guid runId,
        string decisionKey,
        CancellationToken ct = default)
    {
        ScopeContext scope = scopeProvider.GetCurrentScope();
        GraphViewModel? vm = await service.GetDecisionSubgraphAsync(scope, runId, decisionKey, ct);
        return vm is null
            ? this.NotFoundProblem($"Decision graph node '{decisionKey}' for run '{runId}' was not found.", ProblemTypes.ResourceNotFound)
            : Ok(vm);
    }

    /// <summary>Returns the neighbourhood sub-graph around a specific node in the provenance graph.</summary>
    /// <param name="runId">Architecture run id.</param>
    /// <param name="nodeId">Provenance graph node id.</param>
    /// <param name="depth">Hop depth (clamped to [1, 5]).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Neighbourhood sub-graph, or 404 when the run or node is missing.</returns>
    [HttpGet("runs/{runId:guid}/graph/node/{nodeId:guid}")]
    [ProducesResponseType(typeof(GraphViewModel), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(MvcProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetNodeNeighborhood(
        Guid runId,
        Guid nodeId,
        [FromQuery] int depth = 1,
        CancellationToken ct = default)
    {
        depth = Math.Clamp(depth, 1, 5);
        ScopeContext scope = scopeProvider.GetCurrentScope();
        GraphViewModel? vm = await service.GetNodeNeighborhoodAsync(scope, runId, nodeId, depth, ct);
        return vm is null
            ? this.NotFoundProblem($"Provenance node '{nodeId}' for run '{runId}' was not found.", ProblemTypes.ResourceNotFound)
            : Ok(vm);
    }
}
