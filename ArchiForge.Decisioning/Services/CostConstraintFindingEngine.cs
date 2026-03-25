using ArchiForge.Decisioning.Findings.Payloads;
using ArchiForge.Decisioning.Interfaces;
using ArchiForge.Decisioning.Models;
using ArchiForge.KnowledgeGraph.Models;

namespace ArchiForge.Decisioning.Services;

public class CostConstraintFindingEngine : IFindingEngine
{
    public string EngineType => "cost-constraint";
    public string Category => "Cost";

    public Task<IReadOnlyList<Finding>> AnalyzeAsync(
        GraphSnapshot graphSnapshot,
        CancellationToken ct)
    {
        List<Finding> findings = new List<Finding>();

        IReadOnlyList<GraphNode> costNodes = graphSnapshot.GetNodesByType("CostConstraint");

        foreach (GraphNode node in costNodes)
        {
            node.Properties.TryGetValue("budgetName", out string? budgetName);
            node.Properties.TryGetValue("maxMonthlyCost", out string? maxCostStr);
            node.Properties.TryGetValue("costRisk", out string? costRisk);

            decimal? maxMonthly = null;
            if (!string.IsNullOrWhiteSpace(maxCostStr) && decimal.TryParse(maxCostStr, out decimal mc))
                maxMonthly = mc;

            findings.Add(new Finding
            {
                FindingSchemaVersion = FindingsSchema.CurrentFindingVersion,
                FindingType = "CostConstraintFinding",
                Category = Category,
                EngineType = EngineType,
                Severity = string.Equals(costRisk, "high", StringComparison.OrdinalIgnoreCase)
                    ? FindingSeverity.Warning
                    : FindingSeverity.Info,
                Title = $"Cost constraint: {node.Label}",
                Rationale = "A cost constraint node was found and should constrain architecture choices.",
                RelatedNodeIds = [node.NodeId],
                PayloadType = nameof(CostConstraintFindingPayload),
                Payload = new CostConstraintFindingPayload
                {
                    BudgetName = budgetName ?? node.Label,
                    MaxMonthlyCost = maxMonthly,
                    CostRisk = costRisk ?? "unknown"
                },
                Trace = new ExplainabilityTrace
                {
                    GraphNodeIdsExamined = [node.NodeId],
                    DecisionsTaken = ["Emitted cost constraint finding from graph node."]
                }
            });
        }

        return Task.FromResult<IReadOnlyList<Finding>>(findings);
    }
}
