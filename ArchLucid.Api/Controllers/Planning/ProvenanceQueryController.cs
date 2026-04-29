using ArchLucid.Api.Attributes;
using ArchLucid.Api.Models;
using ArchLucid.Api.ProblemDetails;
using ArchLucid.Core.Authorization;
using ArchLucid.Core.Scoping;
using ArchLucid.Core.Tenancy;
using ArchLucid.Persistence.Provenance;
using ArchLucid.Provenance;

using Asp.Versioning;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ArchLucid.Api.Controllers.Planning;

[ApiController]
[Authorize(Policy = ArchLucidPolicies.ReadAuthority)]
[ApiVersion("1.0")]
[Route("v{version:apiVersion}/authority")]
[EnableRateLimiting("fixed")]
[RequiresCommercialTenantTier(TenantTier.Standard)]
public sealed class ProvenanceQueryController(
    IProvenanceSnapshotRepository repo,
    IProvenanceQueryService graphQuery,
    IScopeContextProvider scopeProvider)
    : ControllerBase
{
    /// <summary>
    ///     Returns the persisted provenance snapshot row (raw graph JSON + metadata), not the computed
    ///     <see cref="DecisionProvenanceGraph" />.
    ///     For the structural graph built from run detail, use <c>GET /v1/authority/runs/{runId}/provenance</c> (
    ///     <see cref="AuthorityQueryController" />).
    /// </summary>
    [HttpGet("runs/{runId:guid}/provenance-snapshot")]
    [ProducesResponseType(typeof(DecisionProvenanceSnapshot), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProvenanceSnapshot(Guid runId, CancellationToken ct = default)
    {
        ScopeContext scope = scopeProvider.GetCurrentScope();
        DecisionProvenanceSnapshot? snapshot = await repo.GetByRunIdAsync(scope, runId, ct);
        return snapshot is null
            ? this.NotFoundProblem($"Provenance snapshot for run '{runId}' was not found.",
                ProblemTypes.ResourceNotFound)
            : Ok(snapshot);
    }

    [HttpGet("runs/{runId:guid}/graph")]
    [ProducesResponseType(typeof(GraphViewModel), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetFullGraph(Guid runId, CancellationToken ct = default)
    {
        ScopeContext scope = scopeProvider.GetCurrentScope();
        GraphViewModel? vm = await graphQuery.GetFullGraphAsync(scope, runId, ct);
        return vm is null
            ? this.NotFoundProblem($"Provenance graph for run '{runId}' was not found.", ProblemTypes.ResourceNotFound)
            : Ok(vm);
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
        return vm is null
            ? this.NotFoundProblem($"Decision subgraph '{decisionKey}' for run '{runId}' was not found.",
                ProblemTypes.ResourceNotFound)
            : Ok(vm);
    }

    /// <summary>Neighbourhood sub-graph around a node (authority route; uses persisted graph query).</summary>
    /// <param name="runId">Run that owns the graph.</param>
    /// <param name="nodeId">Graph node id.</param>
    /// <param name="depth">
    ///     Hop depth (clamped to [1, <see cref="ProvenanceQueryLimits.MaxNeighborhoodDepthAuthorityRoute" />
    ///     ]).
    /// </param>
    /// <param name="ct">Cancellation token.</param>
    [HttpGet("runs/{runId:guid}/graph/node/{nodeId:guid}")]
    [ProducesResponseType(typeof(GraphViewModel), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetNodeNeighborhood(
        Guid runId,
        Guid nodeId,
        [FromQuery] int depth = 1,
        CancellationToken ct = default)
    {
        int safeDepth = Math.Clamp(depth, 1, ProvenanceQueryLimits.MaxNeighborhoodDepthAuthorityRoute);
        ScopeContext scope = scopeProvider.GetCurrentScope();
        GraphViewModel? vm = await graphQuery.GetNodeNeighborhoodAsync(scope, runId, nodeId, safeDepth, ct);
        return vm is null
            ? this.NotFoundProblem($"Node '{nodeId}' in run '{runId}' was not found.", ProblemTypes.ResourceNotFound)
            : Ok(vm);
    }
}
