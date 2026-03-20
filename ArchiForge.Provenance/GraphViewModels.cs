namespace ArchiForge.Provenance;

/// <summary>UI-ready graph (e.g. React Flow, Cytoscape): string ids and labels.</summary>
public class GraphViewModel
{
    public List<GraphNodeVm> Nodes { get; set; } = [];
    public List<GraphEdgeVm> Edges { get; set; } = [];
}

public class GraphNodeVm
{
    public string Id { get; set; } = default!;
    public string Label { get; set; } = default!;
    public string Type { get; set; } = default!;
}

public class GraphEdgeVm
{
    public string Source { get; set; } = default!;
    public string Target { get; set; } = default!;
    public string Type { get; set; } = default!;
}
