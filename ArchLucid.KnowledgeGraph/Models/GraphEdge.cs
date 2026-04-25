namespace ArchLucid.KnowledgeGraph.Models;

public class GraphEdge
{
    public string EdgeId
    {
        get;
        set;
    } = null!;

    public string FromNodeId
    {
        get;
        set;
    } = null!;

    public string ToNodeId
    {
        get;
        set;
    } = null!;

    public string EdgeType
    {
        get;
        set;
    } = null!;

    public string? Label
    {
        get;
        set;
    }

    /// <summary>
    ///     Relative strength for traversals and deduplication (explicit ingestion-backed edges use ~1.0; broad heuristics
    ///     lower).
    /// </summary>
    public double Weight
    {
        get;
        set;
    } = 1d;

    public Dictionary<string, string> Properties
    {
        get;
        set;
    } = [];
}
