using ArchLucid.KnowledgeGraph;
using ArchLucid.KnowledgeGraph.Models;

namespace ArchLucid.Decisioning.Analysis;

public class GraphCoverageAnalyzer : IGraphCoverageAnalyzer
{
    public TopologyCoverageResult AnalyzeTopology(GraphSnapshot graphSnapshot)
    {
        IReadOnlyList<GraphNode> topologyNodes = graphSnapshot.GetNodesByType(GraphNodeTypes.TopologyResource);

        List<string> categories = topologyNodes
            .Select(x => x.Category ?? "general")
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        TopologyCoverageResult result = new()
        {
            HasNetwork =
                categories.Exists(x =>
                    x.Equals(GraphTopologyCategories.Network, StringComparison.OrdinalIgnoreCase)),
            HasCompute =
                categories.Exists(x =>
                    x.Equals(GraphTopologyCategories.Compute, StringComparison.OrdinalIgnoreCase)),
            HasStorage =
                categories.Exists(x =>
                    x.Equals(GraphTopologyCategories.Storage, StringComparison.OrdinalIgnoreCase)),
            HasData =
                categories.Exists(x => x.Equals(GraphTopologyCategories.Data, StringComparison.OrdinalIgnoreCase)),
            PresentCategories = categories,
            TopologyNodeCount = topologyNodes.Count,
            TopologyNodeIds = topologyNodes.Select(n => n.NodeId).ToList()
        };

        if (!result.HasNetwork)
            result.MissingCategories.Add(GraphTopologyCategories.Network);
        if (!result.HasCompute)
            result.MissingCategories.Add(GraphTopologyCategories.Compute);
        if (!result.HasStorage)
            result.MissingCategories.Add(GraphTopologyCategories.Storage);
        if (!result.HasData)
            result.MissingCategories.Add(GraphTopologyCategories.Data);

        return result;
    }

    public SecurityCoverageResult AnalyzeSecurity(GraphSnapshot graphSnapshot)
    {
        IReadOnlyList<GraphNode> securityNodes = graphSnapshot.GetNodesByType(GraphNodeTypes.SecurityBaseline);
        IReadOnlyList<GraphNode> topologyNodes = graphSnapshot.GetNodesByType(GraphNodeTypes.TopologyResource);
        IReadOnlyList<GraphEdge> protectsEdges = graphSnapshot.Edges
            .Where(x =>
                string.Equals(x.EdgeType, GraphEdgeTypes.Protects, StringComparison.OrdinalIgnoreCase) &&
                x.Weight >= GraphEdgeDecisioningThresholds.MinWeightForSemanticLink)
            .ToList();

        HashSet<string> protectedIds = protectsEdges
            .Select(x => x.ToNodeId)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        List<string> protectedResources = topologyNodes
            .Where(x => protectedIds.Contains(x.NodeId))
            .Select(x => x.Label)
            .Where(l => !string.IsNullOrWhiteSpace(l))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        List<string> unprotectedResources = topologyNodes
            .Where(x => !protectedIds.Contains(x.NodeId))
            .Select(x => x.Label)
            .Where(l => !string.IsNullOrWhiteSpace(l))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        return new SecurityCoverageResult
        {
            SecurityNodeCount = securityNodes.Count,
            ProtectedResourceCount = protectedResources.Count,
            UnprotectedResourceCount = unprotectedResources.Count,
            ProtectedResources = protectedResources,
            UnprotectedResources = unprotectedResources
        };
    }

    public PolicyCoverageResult AnalyzePolicy(GraphSnapshot graphSnapshot)
    {
        IReadOnlyList<GraphNode> policyNodes = graphSnapshot.GetNodesByType(GraphNodeTypes.PolicyControl);
        IReadOnlyList<GraphNode> topologyNodes = graphSnapshot.GetNodesByType(GraphNodeTypes.TopologyResource);
        IReadOnlyList<GraphEdge> appliesToEdges = graphSnapshot.Edges
            .Where(x =>
                string.Equals(x.EdgeType, GraphEdgeTypes.AppliesTo, StringComparison.OrdinalIgnoreCase) &&
                x.Weight >= GraphEdgeDecisioningThresholds.MinWeightForSemanticLink)
            .ToList();

        HashSet<string> coveredIds = appliesToEdges
            .Select(x => x.ToNodeId)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        List<string> uncoveredResources = topologyNodes
            .Where(x => !coveredIds.Contains(x.NodeId))
            .Select(x => x.Label)
            .Where(l => !string.IsNullOrWhiteSpace(l))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        return new PolicyCoverageResult
        {
            PolicyNodeCount = policyNodes.Count,
            PolicyApplicabilityEdgeCount = appliesToEdges.Count,
            Policies = policyNodes.Select(x => x.Label).Distinct(StringComparer.OrdinalIgnoreCase).ToList(),
            UncoveredResources = uncoveredResources
        };
    }

    public RequirementCoverageResult AnalyzeRequirements(GraphSnapshot graphSnapshot)
    {
        IReadOnlyList<GraphNode> requirementNodes = graphSnapshot.GetNodesByType(GraphNodeTypes.Requirement);
        IReadOnlyList<GraphEdge> relatesToEdges = graphSnapshot.Edges
            .Where(x =>
                string.Equals(x.EdgeType, GraphEdgeTypes.RelatesTo, StringComparison.OrdinalIgnoreCase) &&
                x.Weight >= GraphEdgeDecisioningThresholds.MinWeightForSemanticLink)
            .ToList();

        HashSet<string> coveredIds = relatesToEdges
            .Select(x => x.FromNodeId)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        List<string> coveredRequirements = requirementNodes
            .Where(x => coveredIds.Contains(x.NodeId))
            .Select(x => x.Label)
            .Where(l => !string.IsNullOrWhiteSpace(l))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        List<string> uncoveredRequirements = requirementNodes
            .Where(x => !coveredIds.Contains(x.NodeId))
            .Select(x => x.Label)
            .Where(l => !string.IsNullOrWhiteSpace(l))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        return new RequirementCoverageResult
        {
            RequirementNodeCount = requirementNodes.Count,
            RelatedRequirementCount = coveredRequirements.Count,
            UnrelatedRequirementCount = uncoveredRequirements.Count,
            CoveredRequirements = coveredRequirements,
            UncoveredRequirements = uncoveredRequirements
        };
    }
}
