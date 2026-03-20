using ArchiForge.Decisioning.Findings.Factories;
using ArchiForge.Decisioning.Interfaces;
using ArchiForge.Decisioning.Models;
using ArchiForge.KnowledgeGraph.Models;

namespace ArchiForge.Decisioning.Services;

public class TopologySanityFindingEngine : IFindingEngine
{
    public string EngineType => "topology-sanity";
    public string Category => "Topology";

    public Task<IReadOnlyList<Finding>> AnalyzeAsync(
        GraphSnapshot graphSnapshot,
        CancellationToken ct)
    {
        var findings = new List<Finding>();

        var topologyNodes = graphSnapshot.GetNodesByType("TopologyResource").ToList();

        if (topologyNodes.Count == 0)
        {
            var finding = FindingFactory.CreateTopologyGapFinding(
                engineType: EngineType,
                title: "No topology resources were found",
                rationale: "The graph does not yet contain TopologyResource nodes.",
                gapCode: "TOPOLOGY_MISSING",
                description: "No TopologyResource nodes were present in the current graph snapshot.",
                impact: "The architecture cannot yet support deployment-grade topology decisions.");

            finding.RecommendedActions.Add("Ingest topology resources before architecture synthesis.");
            finding.Trace = new ExplainabilityTrace
            {
                DecisionsTaken = ["Marked graph as incomplete for deployment-level decisions."]
            };

            findings.Add(finding);
        }
        else
        {
            AddCategoryGapFindings(findings, topologyNodes);
        }

        return Task.FromResult<IReadOnlyList<Finding>>(findings);
    }

    private static void AddCategoryGapFindings(List<Finding> findings, List<GraphNode> topologyNodes)
    {
        var categorized = topologyNodes
            .Where(n => !string.IsNullOrWhiteSpace(n.Category))
            .ToList();

        if (categorized.Count == 0)
            return;

        if (!categorized.Any(n => string.Equals(n.Category, "network", StringComparison.OrdinalIgnoreCase)))
        {
            findings.Add(FindingFactory.CreateTopologyGapFinding(
                engineType: "topology-sanity",
                title: "No network-category topology resources",
                rationale: "Deployment-grade topology typically includes at least one network-scoped resource.",
                gapCode: "TOPOLOGY_NETWORK_CATEGORY_MISSING",
                description: "No TopologyResource nodes had category 'network'.",
                impact: "Network segmentation and connectivity decisions may be under-specified."));
        }

        if (!categorized.Any(n => string.Equals(n.Category, "compute", StringComparison.OrdinalIgnoreCase)))
        {
            findings.Add(FindingFactory.CreateTopologyGapFinding(
                engineType: "topology-sanity",
                title: "No compute-category topology resources",
                rationale: "Most architectures declare compute placement (VMs, clusters, app services, etc.).",
                gapCode: "TOPOLOGY_COMPUTE_CATEGORY_MISSING",
                description: "No TopologyResource nodes had category 'compute'.",
                impact: "Workload placement and scaling assumptions may be incomplete."));
        }

        if (!categorized.Any(n =>
                string.Equals(n.Category, "storage", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(n.Category, "data", StringComparison.OrdinalIgnoreCase)))
        {
            findings.Add(FindingFactory.CreateTopologyGapFinding(
                engineType: "topology-sanity",
                title: "No storage or data-category topology resources",
                rationale: "Persistent state is usually represented as storage or data plane resources.",
                gapCode: "TOPOLOGY_STORAGE_OR_DATA_CATEGORY_MISSING",
                description: "No TopologyResource nodes had category 'storage' or 'data'.",
                impact: "Data residency, backup, and compliance scope may be unclear."));
        }
    }
}
