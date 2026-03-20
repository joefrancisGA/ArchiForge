using ArchiForge.Api.Auth.Models;
using ArchiForge.Core.Scoping;
using ArchiForge.KnowledgeGraph.Models;
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
[Route("api/graph")]
[EnableRateLimiting("fixed")]
public sealed class GraphController : ControllerBase
{
    private readonly IAuthorityQueryService _authorityQueryService;
    private readonly IScopeContextProvider _scopeProvider;

    public GraphController(
        IAuthorityQueryService authorityQueryService,
        IScopeContextProvider scopeProvider)
    {
        _authorityQueryService = authorityQueryService;
        _scopeProvider = scopeProvider;
    }

    [HttpGet("runs/{runId:guid}")]
    [ProducesResponseType(typeof(GraphViewModel), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetArchitectureGraph(Guid runId, CancellationToken ct = default)
    {
        var scope = _scopeProvider.GetCurrentScope();
        var detail = await _authorityQueryService.GetRunDetailAsync(scope, runId, ct);
        if (detail?.GraphSnapshot is null)
            return NotFound();

        var vm = MapArchitectureGraph(detail.GraphSnapshot);
        return Ok(vm);
    }

    private static GraphViewModel MapArchitectureGraph(GraphSnapshot snapshot)
    {
        var nodes = snapshot.Nodes.Select(MapNode).ToList();
        var edges = snapshot.Edges.Select(e => new GraphEdgeVm
        {
            Source = e.FromNodeId,
            Target = e.ToNodeId,
            Type = e.EdgeType
        }).ToList();

        return new GraphViewModel { Nodes = nodes, Edges = edges };
    }

    private static GraphNodeVm MapNode(GraphNode x)
    {
        var meta = new Dictionary<string, string>();
        foreach (var kv in x.Properties)
            meta[kv.Key] = kv.Value;

        if (!string.IsNullOrEmpty(x.Category))
            meta["category"] = x.Category!;
        if (!string.IsNullOrEmpty(x.SourceType))
            meta["sourceType"] = x.SourceType!;
        if (!string.IsNullOrEmpty(x.SourceId))
            meta["sourceId"] = x.SourceId!;

        return new GraphNodeVm
        {
            Id = x.NodeId,
            Label = x.Label,
            Type = x.NodeType,
            Metadata = meta.Count > 0 ? meta : null
        };
    }
}
