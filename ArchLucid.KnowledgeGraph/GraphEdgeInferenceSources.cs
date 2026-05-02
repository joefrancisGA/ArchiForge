namespace ArchLucid.KnowledgeGraph;

/// <summary>
///     Values for <see cref="Models.GraphEdge.InferenceSource" /> set by <see cref="Inference.DefaultGraphEdgeInferer" />.
/// </summary>
public static class GraphEdgeInferenceSources
{
    public const string ContextMembership = "context-membership";
    public const string ExplicitParentChild = "explicit-parent-child";
    public const string HeuristicNetworkSubnet = "heuristic-network-subnet";
    public const string PolicyTargeted = "policy-targeted-topology";
    public const string PolicySingleTopologyFallback = "policy-single-topology-fallback";
    public const string RequirementTargeted = "requirement-targeted-topology";
    public const string RequirementTextHeuristic = "requirement-text-heuristic";
    public const string SecurityTargeted = "security-targeted-topology";
    public const string SecuritySingleTopologyFallback = "security-single-topology-fallback";
}
