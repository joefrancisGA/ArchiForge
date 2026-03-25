using ArchiForge.ContextIngestion.Models;
using ArchiForge.KnowledgeGraph.Models;

namespace ArchiForge.KnowledgeGraph.Inference;

public class DefaultGraphEdgeInferer : IGraphEdgeInferer
{
    public IReadOnlyList<GraphEdge> InferEdges(
        ContextSnapshot contextSnapshot,
        IReadOnlyList<GraphNode> nodes)
    {
        ArgumentNullException.ThrowIfNull(contextSnapshot);
        ArgumentNullException.ThrowIfNull(nodes);

        List<GraphEdge> edges = new List<GraphEdge>();

        string contextNodeId = $"context-{contextSnapshot.SnapshotId:N}";
        List<GraphNode> topologyNodes = nodes.Where(x => x.NodeType == GraphNodeTypes.TopologyResource).ToList();
        List<GraphNode> securityNodes = nodes.Where(x => x.NodeType == GraphNodeTypes.SecurityBaseline).ToList();
        List<GraphNode> policyNodes = nodes.Where(x => x.NodeType == GraphNodeTypes.PolicyControl).ToList();
        List<GraphNode> requirementNodes = nodes.Where(x => x.NodeType == GraphNodeTypes.Requirement).ToList();

        foreach (GraphNode node in nodes.Where(x => x.NodeType != GraphNodeTypes.ContextSnapshot))
        {
            edges.Add(CreateEdge(
                contextNodeId,
                node.NodeId,
                GraphEdgeTypes.Contains,
                "contains"));
        }

        InferTopologyContainment(edges, topologyNodes);
        InferSecurityProtection(edges, securityNodes, topologyNodes);
        InferPolicyApplicability(edges, policyNodes, topologyNodes);
        InferRequirementRelevance(edges, requirementNodes, topologyNodes);

        return Deduplicate(edges);
    }

    private static void InferTopologyContainment(
        List<GraphEdge> edges,
        List<GraphNode> topologyNodes)
    {
        List<GraphNode> networks = topologyNodes
            .Where(x => string.Equals(x.Category, GraphTopologyCategories.Network, StringComparison.OrdinalIgnoreCase))
            .ToList();

        List<GraphNode> subnets = topologyNodes
            .Where(x => x.Label.Contains("subnet", StringComparison.OrdinalIgnoreCase))
            .ToList();

        foreach (GraphNode network in networks)
        {
            foreach (GraphNode subnet in subnets)
            {
                edges.Add(CreateEdge(
                    network.NodeId,
                    subnet.NodeId,
                    GraphEdgeTypes.ContainsResource,
                    "contains resource"));
            }
        }
    }

    private static void InferSecurityProtection(
        List<GraphEdge> edges,
        List<GraphNode> securityNodes,
        List<GraphNode> topologyNodes)
    {
        foreach (GraphNode security in securityNodes)
        {
            foreach (GraphNode resource in topologyNodes)
            {
                edges.Add(CreateEdge(
                    security.NodeId,
                    resource.NodeId,
                    GraphEdgeTypes.Protects,
                    "protects"));
            }
        }
    }

    private static void InferPolicyApplicability(
        List<GraphEdge> edges,
        List<GraphNode> policyNodes,
        List<GraphNode> topologyNodes)
    {
        foreach (GraphNode policy in policyNodes)
        {
            foreach (GraphNode resource in topologyNodes)
            {
                edges.Add(CreateEdge(
                    policy.NodeId,
                    resource.NodeId,
                    GraphEdgeTypes.AppliesTo,
                    "applies to"));
            }
        }
    }

    private static void InferRequirementRelevance(
        List<GraphEdge> edges,
        List<GraphNode> requirementNodes,
        List<GraphNode> topologyNodes)
    {
        foreach (GraphNode requirement in requirementNodes)
        {
            string requirementText = requirement.Properties.TryGetValue("text", out string? text)
                ? text
                : requirement.Label;

            foreach (GraphNode resource in topologyNodes)
            {
                if (LooksRelevant(requirementText, resource))
                {
                    edges.Add(CreateEdge(
                        requirement.NodeId,
                        resource.NodeId,
                        GraphEdgeTypes.RelatesTo,
                        "relates to"));
                }
            }
        }
    }

    private static bool LooksRelevant(string requirementText, GraphNode resource)
    {
        string text = requirementText.ToLowerInvariant();
        string label = resource.Label.ToLowerInvariant();
        string category = resource.Category?.ToLowerInvariant() ?? string.Empty;

        if (text.Contains("network", StringComparison.Ordinal) && (label.Contains("vnet", StringComparison.Ordinal) || label.Contains("subnet", StringComparison.Ordinal) || string.Equals(category, GraphTopologyCategories.Network, StringComparison.OrdinalIgnoreCase)))
            return true;

        if (text.Contains("storage", StringComparison.Ordinal) && string.Equals(category, GraphTopologyCategories.Storage, StringComparison.OrdinalIgnoreCase))
            return true;

        if (text.Contains("compute", StringComparison.Ordinal) && string.Equals(category, GraphTopologyCategories.Compute, StringComparison.OrdinalIgnoreCase))
            return true;

        if (text.Contains("security", StringComparison.Ordinal) && resource.NodeType == GraphNodeTypes.SecurityBaseline)
            return true;

        if (text.Contains("database", StringComparison.Ordinal) && string.Equals(category, GraphTopologyCategories.Data, StringComparison.OrdinalIgnoreCase))
            return true;

        return false;
    }

    private static GraphEdge CreateEdge(
        string fromNodeId,
        string toNodeId,
        string edgeType,
        string label)
    {
        return new GraphEdge
        {
            EdgeId = Guid.NewGuid().ToString("N"),
            FromNodeId = fromNodeId,
            ToNodeId = toNodeId,
            EdgeType = edgeType,
            Label = label
        };
    }

    private static IReadOnlyList<GraphEdge> Deduplicate(IEnumerable<GraphEdge> edges)
    {
        return edges
            .GroupBy(
                x => $"{x.FromNodeId}|{x.ToNodeId}|{x.EdgeType}",
                StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First())
            .ToList();
    }
}
