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
        var findings = new List<Finding>();

        var costNodes = graphSnapshot.Nodes
            .Where(n => n.NodeType == "CostConstraint")
            .ToList();

        foreach (var node in costNodes)
        {
            node.Properties.TryGetValue("budgetName", out var budgetName);
            node.Properties.TryGetValue("maxMonthlyCost", out var maxCostStr);
            node.Properties.TryGetValue("costRisk", out var costRisk);

            decimal? maxMonthly = null;
            if (!string.IsNullOrWhiteSpace(maxCostStr) && decimal.TryParse(maxCostStr, out var mc))
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
