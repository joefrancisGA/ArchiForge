using ArchLucid.Core.Pagination;

namespace ArchLucid.KnowledgeGraph.Models;

/// <summary>
///     Slices <see cref="GraphSnapshot.Nodes" /> with stable list order; edges are restricted to the page’s node id set.
/// </summary>
public static class GraphSnapshotPagination
{
    /// <summary>Builds a page; <paramref name="snapshot" /> must not be null.</summary>
    public static GraphSnapshotNodesPage CreatePage(GraphSnapshot snapshot, int page, int pageSize)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        (int safePage, int safeSize) = PaginationDefaults.Normalize(page, pageSize);
        IReadOnlyList<GraphNode> allNodes = snapshot.Nodes;
        int total = allNodes.Count;
        int skip = PaginationDefaults.ToSkip(safePage, safeSize);
        List<GraphNode> slice = allNodes.Skip(skip).Take(safeSize).ToList();
        HashSet<string> ids = slice.Select(static n => n.NodeId).ToHashSet(StringComparer.Ordinal);
        List<GraphEdge> edges = snapshot.Edges
            .Where(e => ids.Contains(e.FromNodeId) && ids.Contains(e.ToNodeId))
            .ToList();

        return new GraphSnapshotNodesPage
        {
            Page = safePage,
            PageSize = safeSize,
            TotalNodes = total,
            HasMore = safePage * safeSize < total,
            Nodes = slice,
            Edges = edges
        };
    }
}
