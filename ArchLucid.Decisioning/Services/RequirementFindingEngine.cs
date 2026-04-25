using ArchLucid.Decisioning.Findings.Factories;
using ArchLucid.Decisioning.Interfaces;
using ArchLucid.Decisioning.Models;
using ArchLucid.KnowledgeGraph.Models;

namespace ArchLucid.Decisioning.Services;

public class RequirementFindingEngine : IFindingEngine
{
    public string EngineType => "requirement";
    public string Category => "Requirement";

    public Task<IReadOnlyList<Finding>> AnalyzeAsync(
        GraphSnapshot graphSnapshot,
        CancellationToken ct)
    {
        List<Finding> findings = [];

        IReadOnlyList<GraphNode> requirementNodes = graphSnapshot.GetNodesByType("Requirement");

        foreach (GraphNode node in requirementNodes)
        {
            node.Properties.TryGetValue("text", out string? requirementText);

            List<string> relatedFromGraph = graphSnapshot
                .GetOutgoingTargets(node.NodeId, "RELATES_TO")
                .Select(n => n.NodeId)
                .ToList();

            List<string> relatedNodeIds = [node.NodeId];

            foreach (string id in relatedFromGraph.Where(id =>
                         !relatedNodeIds.Contains(id, StringComparer.OrdinalIgnoreCase)))

                relatedNodeIds.Add(id);


            Finding finding = FindingFactory.CreateRequirementFinding(
                EngineType,
                $"Requirement detected: {node.Label}",
                "A requirement node exists and must be reflected in the resolved architecture.",
                node.Label,
                requirementText ?? string.Empty,
                true,
                relatedNodeIds);

            finding.RecommendedActions.Add("Carry this requirement into the GoldenManifest.");
            string text = requirementText ?? string.Empty;

            finding.Trace = new ExplainabilityTrace
            {
                GraphNodeIdsExamined = relatedNodeIds,
                RulesApplied = ["requirement-surface"],
                DecisionsTaken =
                [
                    relatedFromGraph.Count > 0
                        ? "Linked requirement to topology resources via RELATES_TO graph edges."
                        : "Promote requirement into candidate architecture decision input."
                ],
                Notes =
                [
                    $"Related topology resources: {relatedFromGraph.Count}",
                    string.IsNullOrWhiteSpace(text)
                        ? "No requirement text provided."
                        : $"Requirement text length: {text.Length} chars"
                ]
            };

            findings.Add(finding);
        }

        return Task.FromResult<IReadOnlyList<Finding>>(findings);
    }
}
