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

        var topologyNodes = graphSnapshot.Nodes
            .Where(n => n.NodeType == "TopologyResource")
            .ToList();

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

        return Task.FromResult<IReadOnlyList<Finding>>(findings);
    }
}

