using ArchiForge.KnowledgeGraph;
using ArchiForge.KnowledgeGraph.Models;

namespace ArchiForge.Decisioning.Analysis;

public class GraphCoverageAnalyzer : IGraphCoverageAnalyzer
{
    public TopologyCoverageResult AnalyzeTopology(GraphSnapshot graphSnapshot)
    {
        var topologyNodes = graphSnapshot.GetNodesByType(GraphNodeTypes.TopologyResource);

        var categories = topologyNodes
            .Select(x => x.Category ?? "general")
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var result = new TopologyCoverageResult
        {
            HasNetwork = categories.Exists(x => x.Equals(GraphTopologyCategories.Network, StringComparison.OrdinalIgnoreCase)),
            HasCompute = categories.Exists(x => x.Equals(GraphTopologyCategories.Compute, StringComparison.OrdinalIgnoreCase)),
            HasStorage = categories.Exists(x => x.Equals(GraphTopologyCategories.Storage, StringComparison.OrdinalIgnoreCase)),
            HasData = categories.Exists(x => x.Equals(GraphTopologyCategories.Data, StringComparison.OrdinalIgnoreCase)),
            PresentCategories = categories,
            TopologyNodeCount = topologyNodes.Count
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
        var securityNodes = graphSnapshot.GetNodesByType(GraphNodeTypes.SecurityBaseline);
        var topologyNodes = graphSnapshot.GetNodesByType(GraphNodeTypes.TopologyResource);
        var protectsEdges = graphSnapshot.GetEdgesByType(GraphEdgeTypes.Protects);

        var protectedIds = protectsEdges
            .Select(x => x.ToNodeId)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var protectedResources = topologyNodes
            .Where(x => protectedIds.Contains(x.NodeId))
            .Select(x => x.Label)
            .Where(l => !string.IsNullOrWhiteSpace(l))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var unprotectedResources = topologyNodes
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
        var policyNodes = graphSnapshot.GetNodesByType(GraphNodeTypes.PolicyControl);
        var topologyNodes = graphSnapshot.GetNodesByType(GraphNodeTypes.TopologyResource);
        var appliesToEdges = graphSnapshot.GetEdgesByType(GraphEdgeTypes.AppliesTo);

        var coveredIds = appliesToEdges
            .Select(x => x.ToNodeId)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var uncoveredResources = topologyNodes
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
        var requirementNodes = graphSnapshot.GetNodesByType(GraphNodeTypes.Requirement);
        var relatesToEdges = graphSnapshot.GetEdgesByType(GraphEdgeTypes.RelatesTo);

        var coveredIds = relatesToEdges
            .Select(x => x.FromNodeId)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var coveredRequirements = requirementNodes
            .Where(x => coveredIds.Contains(x.NodeId))
            .Select(x => x.Label)
            .Where(l => !string.IsNullOrWhiteSpace(l))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var uncoveredRequirements = requirementNodes
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
