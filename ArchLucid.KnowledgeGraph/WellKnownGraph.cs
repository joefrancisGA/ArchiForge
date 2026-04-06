namespace ArchiForge.KnowledgeGraph;

/// <summary>
/// Canonical <see cref="Models.GraphNode.NodeType"/> values produced by the default builder and inferrer.
/// </summary>
public static class GraphNodeTypes
{
    public const string ContextSnapshot = "ContextSnapshot";
    public const string TopologyResource = "TopologyResource";
    public const string SecurityBaseline = "SecurityBaseline";
    public const string PolicyControl = "PolicyControl";
    public const string Requirement = "Requirement";
}

/// <summary>
/// Canonical <see cref="Models.GraphEdge.EdgeType"/> values inferred by <see cref="Inference.DefaultGraphEdgeInferer"/>.
/// </summary>
public static class GraphEdgeTypes
{
    public const string Contains = "CONTAINS";
    public const string ContainsResource = "CONTAINS_RESOURCE";
    public const string Protects = "PROTECTS";
    public const string AppliesTo = "APPLIES_TO";
    public const string RelatesTo = "RELATES_TO";
}

/// <summary>
/// Typical <see cref="Models.GraphNode.Category"/> values for topology resources (enrichment / heuristics).
/// </summary>
public static class GraphTopologyCategories
{
    public const string Network = "network";
    public const string Storage = "storage";
    public const string Compute = "compute";
    public const string Data = "data";
}
