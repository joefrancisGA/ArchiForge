namespace ArchLucid.KnowledgeGraph;

/// <summary>
///     Optional <see cref="ArchLucid.ContextIngestion.Models.CanonicalObject.Properties" /> keys
///     that narrow graph inference when set. Values are comma-separated <see cref="Models.GraphNode.NodeId" />
///     strings (e.g. <c>obj-abc123</c>, <c>context-…</c> is not a topology target).
/// </summary>
/// <remarks>
///     When absent, <see cref="Inference.DefaultGraphEdgeInferer" /> falls back to legacy heuristics
///     (all policies → all topology, text-based requirement relevance).
/// </remarks>
public static class CanonicalGraphPropertyKeys
{
    /// <summary>Policy control nodes: explicit topology resources this policy applies to.</summary>
    public const string ApplicableTopologyNodeIds = "applicableTopologyNodeIds";

    /// <summary>Requirement nodes: explicit topology resources this requirement relates to.</summary>
    public const string RelatedTopologyNodeIds = "relatedTopologyNodeIds";
}
