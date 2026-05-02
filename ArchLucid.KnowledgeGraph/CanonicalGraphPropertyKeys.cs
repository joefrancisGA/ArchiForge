namespace ArchLucid.KnowledgeGraph;

/// <summary>
///     Optional <see cref="ArchLucid.ContextIngestion.Models.CanonicalObject.Properties" /> keys
///     that narrow graph inference when set. Values are comma-separated <see cref="Models.GraphNode.NodeId" />
///     strings (e.g. <c>obj-abc123</c>, <c>context-…</c> is not a topology target).
/// </summary>
/// <remarks>
///     When absent for policies or security baselines, inference only links a single topology anchor (one resource) or
///     skips scope edges until explicit IDs are provided. Requirements still use a narrow text heuristic when
///     <see cref="RelatedTopologyNodeIds" /> is unset.
/// </remarks>
public static class CanonicalGraphPropertyKeys
{
    /// <summary>Policy control nodes: explicit topology resources this policy applies to.</summary>
    public const string ApplicableTopologyNodeIds = "applicableTopologyNodeIds";

    /// <summary>Requirement nodes: explicit topology resources this requirement relates to.</summary>
    public const string RelatedTopologyNodeIds = "relatedTopologyNodeIds";

    /// <summary>Security baseline nodes: explicit topology resources in scope for this control.</summary>
    public const string ProtectedTopologyNodeIds = "protectedTopologyNodeIds";
}
