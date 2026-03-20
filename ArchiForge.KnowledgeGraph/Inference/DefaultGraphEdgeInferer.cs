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

        var edges = new List<GraphEdge>();

        var contextNodeId = $"context-{contextSnapshot.SnapshotId:N}";
        var topologyNodes = nodes.Where(x => x.NodeType == "TopologyResource").ToList();
        var securityNodes = nodes.Where(x => x.NodeType == "SecurityBaseline").ToList();
        var policyNodes = nodes.Where(x => x.NodeType == "PolicyControl").ToList();
        var requirementNodes = nodes.Where(x => x.NodeType == "Requirement").ToList();

        foreach (var node in nodes.Where(x => x.NodeType != "ContextSnapshot"))
        {
            edges.Add(CreateEdge(
                contextNodeId,
                node.NodeId,
                "CONTAINS",
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
        var networks = topologyNodes
            .Where(x => string.Equals(x.Category, "network", StringComparison.OrdinalIgnoreCase))
            .ToList();

        var subnets = topologyNodes
            .Where(x => x.Label.Contains("subnet", StringComparison.OrdinalIgnoreCase))
            .ToList();

        foreach (var network in networks)
        {
            foreach (var subnet in subnets)
            {
                edges.Add(CreateEdge(
                    network.NodeId,
                    subnet.NodeId,
                    "CONTAINS_RESOURCE",
                    "contains resource"));
            }
        }
    }

    private static void InferSecurityProtection(
        List<GraphEdge> edges,
        List<GraphNode> securityNodes,
        List<GraphNode> topologyNodes)
    {
        foreach (var security in securityNodes)
        {
            foreach (var resource in topologyNodes)
            {
                edges.Add(CreateEdge(
                    security.NodeId,
                    resource.NodeId,
                    "PROTECTS",
                    "protects"));
            }
        }
    }

    private static void InferPolicyApplicability(
        List<GraphEdge> edges,
        List<GraphNode> policyNodes,
        List<GraphNode> topologyNodes)
    {
        foreach (var policy in policyNodes)
        {
            foreach (var resource in topologyNodes)
            {
                edges.Add(CreateEdge(
                    policy.NodeId,
                    resource.NodeId,
                    "APPLIES_TO",
                    "applies to"));
            }
        }
    }

    private static void InferRequirementRelevance(
        List<GraphEdge> edges,
        List<GraphNode> requirementNodes,
        List<GraphNode> topologyNodes)
    {
        foreach (var requirement in requirementNodes)
        {
            var requirementText = requirement.Properties.TryGetValue("text", out var text)
                ? text
                : requirement.Label;

            foreach (var resource in topologyNodes)
            {
                if (LooksRelevant(requirementText, resource))
                {
                    edges.Add(CreateEdge(
                        requirement.NodeId,
                        resource.NodeId,
                        "RELATES_TO",
                        "relates to"));
                }
            }
        }
    }

    private static bool LooksRelevant(string requirementText, GraphNode resource)
    {
        var text = requirementText.ToLowerInvariant();
        var label = resource.Label.ToLowerInvariant();
        var category = resource.Category?.ToLowerInvariant() ?? string.Empty;

        if (text.Contains("network", StringComparison.Ordinal) && (label.Contains("vnet", StringComparison.Ordinal) || label.Contains("subnet", StringComparison.Ordinal) || category == "network"))
            return true;

        if (text.Contains("storage", StringComparison.Ordinal) && category == "storage")
            return true;

        if (text.Contains("compute", StringComparison.Ordinal) && category == "compute")
            return true;

        if (text.Contains("security", StringComparison.Ordinal) && resource.NodeType == "SecurityBaseline")
            return true;

        if (text.Contains("database", StringComparison.Ordinal) && category == "data")
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
