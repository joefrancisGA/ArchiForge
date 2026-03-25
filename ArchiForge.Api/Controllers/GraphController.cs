using ArchiForge.Api.Auth.Models;
using ArchiForge.Api.ProblemDetails;
using ArchiForge.Core.Scoping;
using ArchiForge.KnowledgeGraph.Models;
using ArchiForge.Persistence.Queries;
using ArchiForge.Provenance;

using Asp.Versioning;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ArchiForge.Api.Controllers;

/// <summary>
/// HTTP API for retrieving the architecture knowledge graph snapshot associated with a run.
/// </summary>
/// <remarks>
/// Routes are prefixed <c>api/graph</c> and require the <see cref="ArchiForgePolicies.ReadAuthority"/> policy.
/// The graph is projected from the <see cref="ArchiForge.KnowledgeGraph.Models.GraphSnapshot"/> stored in the
/// canonical run detail and returned as a <see cref="GraphViewModel"/> with typed node and edge view models.
/// </remarks>
[ApiController]
[Authorize(Policy = ArchiForgePolicies.ReadAuthority)]
[ApiVersion("1.0")]
[Route("api/graph")]
[EnableRateLimiting("fixed")]
public sealed class GraphController(
    IAuthorityQueryService authorityQueryService,
    IScopeContextProvider scopeProvider)
    : ControllerBase
{
    [HttpGet("runs/{runId:guid}")]
    [ProducesResponseType(typeof(GraphViewModel), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetArchitectureGraph(Guid runId, CancellationToken ct = default)
    {
        ScopeContext scope = scopeProvider.GetCurrentScope();
        RunDetailDto? detail = await authorityQueryService.GetRunDetailAsync(scope, runId, ct);
        if (detail is null)
            return this.NotFoundProblem($"Run '{runId}' was not found.", ProblemTypes.RunNotFound);
        if (detail.GraphSnapshot is null)
            return this.NotFoundProblem($"Run '{runId}' does not have a graph snapshot.", ProblemTypes.ResourceNotFound);

        GraphViewModel vm = MapArchitectureGraph(detail.GraphSnapshot);
        return Ok(vm);
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

    private static GraphNodeVm MapNode(GraphNode x)
    {
        Dictionary<string, string> meta = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

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
