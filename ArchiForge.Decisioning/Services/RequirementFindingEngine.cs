using ArchiForge.Decisioning.Findings.Factories;
using ArchiForge.Decisioning.Interfaces;
using ArchiForge.Decisioning.Models;
using ArchiForge.KnowledgeGraph.Models;

namespace ArchiForge.Decisioning.Services;

public class RequirementFindingEngine : IFindingEngine
{
    public string EngineType => "requirement";
    public string Category => "Requirement";

    public Task<IReadOnlyList<Finding>> AnalyzeAsync(
        GraphSnapshot graphSnapshot,
        CancellationToken ct)
    {
        var findings = new List<Finding>();

        var requirementNodes = graphSnapshot.GetNodesByType("Requirement");

        foreach (var node in requirementNodes)
        {
            node.Properties.TryGetValue("text", out var requirementText);

            var relatedFromGraph = graphSnapshot
                .GetOutgoingTargets(node.NodeId, "RELATES_TO")
                .Select(n => n.NodeId)
                .ToList();

            var relatedNodeIds = new List<string> { node.NodeId };
            foreach (var id in relatedFromGraph)
            {
                if (!relatedNodeIds.Contains(id, StringComparer.OrdinalIgnoreCase))
                    relatedNodeIds.Add(id);
            }

            var finding = FindingFactory.CreateRequirementFinding(
                engineType: EngineType,
                title: $"Requirement detected: {node.Label}",
                rationale: "A requirement node exists and must be reflected in the resolved architecture.",
                requirementName: node.Label,
                requirementText: requirementText ?? string.Empty,
                isMandatory: true,
                relatedNodeIds: relatedNodeIds);

            finding.RecommendedActions.Add("Carry this requirement into the GoldenManifest.");
            finding.Trace = new ExplainabilityTrace
            {
                GraphNodeIdsExamined = relatedNodeIds,
                DecisionsTaken =
                [
                    relatedFromGraph.Count > 0
                        ? "Linked requirement to topology resources via RELATES_TO graph edges."
                        : "Promote requirement into candidate architecture decision input."
                ]
            };

            findings.Add(finding);
        }

        return Task.FromResult<IReadOnlyList<Finding>>(findings);
    }
}
