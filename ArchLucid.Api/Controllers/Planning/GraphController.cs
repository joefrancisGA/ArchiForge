using ArchLucid.Api.Attributes;
using ArchLucid.Api.ProblemDetails;
using ArchLucid.Core.Authorization;
using ArchLucid.Core.Pagination;
using ArchLucid.Core.Scoping;
using ArchLucid.Core.Tenancy;
using ArchLucid.KnowledgeGraph.Configuration;
using ArchLucid.KnowledgeGraph.Models;
using ArchLucid.Persistence.Queries;
using ArchLucid.Provenance;

using Asp.Versioning;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;

namespace ArchLucid.Api.Controllers.Planning;

/// <summary>
///     HTTP API for retrieving the architecture knowledge graph snapshot associated with a run.
/// </summary>
/// <remarks>
///     Routes are prefixed <c>api/graph</c> and require the <see cref="ArchLucidPolicies.ReadAuthority" /> policy.
///     The graph is projected from the <see cref="ArchLucid.KnowledgeGraph.Models.GraphSnapshot" /> stored in the
///     canonical run detail and returned as a <see cref="GraphViewModel" /> with typed node and edge view models.
/// </remarks>
[ApiController]
[Authorize(Policy = ArchLucidPolicies.ReadAuthority)]
[ApiVersion("1.0")]
[Route("v{version:apiVersion}/graph")]
[EnableRateLimiting("fixed")]
[RequiresCommercialTenantTier(TenantTier.Standard)]
public sealed class GraphController(
    IAuthorityQueryService authorityQueryService,
    IScopeContextProvider scopeProvider,
    IOptions<KnowledgeGraphLimitsOptions> knowledgeGraphLimits)
    : ControllerBase
{
    /// <summary>
    ///     Returns a <see cref="GraphViewModel" /> for <paramref name="runId" /> when a graph snapshot exists in the caller’s
    ///     scope.
    /// </summary>
    [HttpGet("runs/{runId:guid}")]
    [ProducesResponseType(typeof(GraphViewModel), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status413PayloadTooLarge)]
    public async Task<IActionResult> GetArchitectureGraph(Guid runId, CancellationToken ct = default)
    {
        ScopeContext scope = scopeProvider.GetCurrentScope();
        RunDetailDto? detail = await authorityQueryService.GetRunDetailAsync(scope, runId, ct);
        if (detail is null)
            return this.NotFoundProblem($"Run '{runId}' was not found.", ProblemTypes.RunNotFound);
        if (detail.GraphSnapshot is null)
            return this.NotFoundProblem($"Run '{runId}' does not have a graph snapshot.",
                ProblemTypes.ResourceNotFound);

        KnowledgeGraphLimitsOptions limits = knowledgeGraphLimits.Value;

        if (limits.FullGraphResponseMaxNodes > 0 &&
            detail.GraphSnapshot.Nodes.Count > limits.FullGraphResponseMaxNodes)
        {
            return this.PayloadTooLargeProblem(
                $"This graph has {detail.GraphSnapshot.Nodes.Count} nodes; the full-graph endpoint allows at most "
                + $"{limits.FullGraphResponseMaxNodes}. Use GET /v1/graph/runs/{runId}/nodes with page and pageSize "
                + $"(maximum page size {PaginationDefaults.MaxPageSize}).",
                ProblemTypes.GraphTooLargeForFullResponse);
        }

        GraphViewModel vm = MapArchitectureGraph(detail.GraphSnapshot);
        return Ok(vm);
    }

    /// <summary>
    ///     Returns a page of graph nodes (stable snapshot order) and edges whose endpoints both appear on that page.
    /// </summary>
    [HttpGet("runs/{runId:guid}/nodes")]
    [ProducesResponseType(typeof(GraphNodesPageResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetArchitectureGraphNodesPage(
        Guid runId,
        [FromQuery] int page = PaginationDefaults.DefaultPage,
        [FromQuery] int pageSize = PaginationDefaults.DefaultPageSize,
        CancellationToken ct = default)
    {
        ScopeContext scope = scopeProvider.GetCurrentScope();
        RunDetailDto? detail = await authorityQueryService.GetRunDetailAsync(scope, runId, ct);
        if (detail is null)
            return this.NotFoundProblem($"Run '{runId}' was not found.", ProblemTypes.RunNotFound);
        if (detail.GraphSnapshot is null)
            return this.NotFoundProblem($"Run '{runId}' does not have a graph snapshot.",
                ProblemTypes.ResourceNotFound);

        GraphSnapshotNodesPage slice = GraphSnapshotPagination.CreatePage(detail.GraphSnapshot, page, pageSize);
        GraphNodesPageResponse body = MapArchitectureGraphPage(slice);
        return Ok(body);
    }

    private static GraphViewModel MapArchitectureGraph(GraphSnapshot snapshot)
    {
        List<GraphNodeVm> nodes = snapshot.Nodes.Select(MapNode).ToList();
        List<GraphEdgeVm> edges = snapshot.Edges.Select(e => new GraphEdgeVm
        {
            Source = e.FromNodeId,
            Target = e.ToNodeId,
            Type = e.EdgeType
        }).ToList();

        return new GraphViewModel { Nodes = nodes, Edges = edges };
    }

    private static GraphNodesPageResponse MapArchitectureGraphPage(GraphSnapshotNodesPage slice)
    {
        List<GraphNodeVm> nodes = slice.Nodes.Select(MapNode).ToList();
        List<GraphEdgeVm> edges = slice.Edges
            .Select(e => new GraphEdgeVm { Source = e.FromNodeId, Target = e.ToNodeId, Type = e.EdgeType })
            .ToList();

        return new GraphNodesPageResponse
        {
            Page = slice.Page,
            PageSize = slice.PageSize,
            TotalNodes = slice.TotalNodes,
            HasMore = slice.HasMore,
            Nodes = nodes,
            Edges = edges
        };
    }

    private static GraphNodeVm MapNode(GraphNode x)
    {
        Dictionary<string, string> meta = new(StringComparer.OrdinalIgnoreCase);

        // Known structured fields take priority over raw property bag entries.
        if (!string.IsNullOrEmpty(x.Category))
            meta["category"] = x.Category;
        if (!string.IsNullOrEmpty(x.SourceType))
            meta["sourceType"] = x.SourceType;
        if (!string.IsNullOrEmpty(x.SourceId))
            meta["sourceId"] = x.SourceId;

        // Additional properties are merged; duplicate keys from Properties are skipped.
        foreach (KeyValuePair<string, string> kv in x.Properties)
            meta.TryAdd(kv.Key, kv.Value);

        return new GraphNodeVm
        {
            Id = x.NodeId,
            Label = x.Label,
            Type = x.NodeType,
            Metadata = meta.Count > 0 ? meta : null
        };
    }
}
