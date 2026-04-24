namespace ArchLucid.KnowledgeGraph.Models;

public class GraphBuildResult
{
    public List<GraphNode> Nodes
    {
        get;
        set;
    } = [];

    public List<GraphEdge> Edges
    {
        get;
        set;
    } = [];

    public List<string> Warnings
    {
        get;
        set;
    } = [];
}
