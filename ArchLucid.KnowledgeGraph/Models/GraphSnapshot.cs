namespace ArchLucid.KnowledgeGraph.Models;

public class GraphSnapshot
{
    /// <summary>JSON contract version for forward-compatible deserialization (default <c>1</c>).</summary>
    public int SchemaVersion
    {
        get;
        set;
    } = 1;

    public Guid GraphSnapshotId
    {
        get;
        set;
    }

    public Guid ContextSnapshotId
    {
        get;
        set;
    }

    public Guid RunId
    {
        get;
        set;
    }

    public DateTime CreatedUtc
    {
        get;
        set;
    }

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
