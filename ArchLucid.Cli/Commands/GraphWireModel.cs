namespace ArchLucid.Cli.Commands;

/// <summary>Wire shape for provenance REST graph JSON.</summary>
internal sealed class GraphWireModel
{
    public List<GraphNodeWire> Nodes
    {
        get;
        set;
    } = [];

    public List<GraphEdgeWire> Edges
    {
        get;
        set;
    } = [];
}
