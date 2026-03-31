namespace ArchiForge.KnowledgeGraph.Models;

/// <summary>
/// One page of nodes from a <see cref="GraphSnapshot"/> plus edges whose endpoints both lie in that page.
/// </summary>
public sealed class GraphSnapshotNodesPage
{
    public int Page { get; init; }

    public int PageSize { get; init; }

    public int TotalNodes { get; init; }

    public bool HasMore { get; init; }

    public IReadOnlyList<GraphNode> Nodes { get; init; } = [];

    public IReadOnlyList<GraphEdge> Edges { get; init; } = [];
}
