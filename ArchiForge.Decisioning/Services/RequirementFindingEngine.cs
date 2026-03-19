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

        var requirementNodes = graphSnapshot.Nodes
            .Where(n => n.NodeType == "Requirement")
            .ToList();

        foreach (var node in requirementNodes)
        {
            node.Properties.TryGetValue("text", out var requirementText);

            var finding = FindingFactory.CreateRequirementFinding(
                engineType: EngineType,
                title: $"Requirement detected: {node.Label}",
                rationale: "A requirement node exists and must be reflected in the resolved architecture.",
                requirementName: node.Label,
                requirementText: requirementText ?? string.Empty,
                isMandatory: true,
                relatedNodeIds: [node.NodeId]);

            finding.RecommendedActions.Add("Carry this requirement into the GoldenManifest.");
            finding.Trace = new ExplainabilityTrace
            {
                GraphNodeIdsExamined = [node.NodeId],
                DecisionsTaken = ["Promote requirement into candidate architecture decision input."]
            };

            findings.Add(finding);
        }

        return Task.FromResult<IReadOnlyList<Finding>>(findings);
    }
}

