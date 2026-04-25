namespace ArchLucid.Provenance;

/// <summary>UI-ready graph (e.g. React Flow, Cytoscape): string ids and labels.</summary>
public class GraphViewModel
{
    public List<GraphNodeVm> Nodes
    {
        get;
        set;
    } = [];

    public List<GraphEdgeVm> Edges
    {
        get;
        set;
    } = [];

    /// <summary>Serialized for JSON clients (provenance and architecture graph endpoints).</summary>
    public int NodeCount => Nodes.Count;

    /// <summary>Serialized edge count for empty-state and layout hints in operator UIs.</summary>
    public int EdgeCount => Edges.Count;

    /// <summary>True when there are no nodes (explicit empty graph).</summary>
    public bool IsEmpty => Nodes.Count == 0;
}

public class GraphNodeVm
{
    public string Id
    {
        get;
        set;
    } = null!;

    public string Label
    {
        get;
        set;
    } = null!;

    public string Type
    {
        get;
        set;
    } = null!;

    /// <summary>Optional key/value pairs for UI detail panel (provenance metadata, graph properties).</summary>
    public Dictionary<string, string>? Metadata
    {
        get;
        set;
    }
}

public class GraphEdgeVm
{
    public string Source
    {
        get;
        set;
    } = null!;

    public string Target
    {
        get;
        set;
    } = null!;

    public string Type
    {
        get;
        set;
    } = null!;
}

/// <summary>
///     Paginated architecture graph (same node/edge VM shapes as <see cref="GraphViewModel" />).
/// </summary>
public sealed class GraphNodesPageResponse
{
    public int Page
    {
        get;
        set;
    }

    public int PageSize
    {
        get;
        set;
    }

    public int TotalNodes
    {
        get;
        set;
    }

    public bool HasMore
    {
        get;
        set;
    }

    public List<GraphNodeVm> Nodes
    {
        get;
        set;
    } = [];

    public List<GraphEdgeVm> Edges
    {
        get;
        set;
    } = [];
}
