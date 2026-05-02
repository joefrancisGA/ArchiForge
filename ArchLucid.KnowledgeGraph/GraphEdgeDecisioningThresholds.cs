namespace ArchLucid.KnowledgeGraph;

/// <summary>
///     Minimum edge weights used when expanding semantic graph scope (policy applicability, security protection,
///     requirement links). Edges below this are still stored for visualization but are ignored by decisioning so broad
///     heuristics cannot silently mark workloads compliant or protected.
/// </summary>
public static class GraphEdgeDecisioningThresholds
{
    /// <summary>Inclusive minimum <see cref="Models.GraphEdge.Weight" /> for PROTECTS / APPLIES_TO / RELATES_TO traversals in engines.</summary>
    public const double MinWeightForSemanticLink = 0.5d;
}
