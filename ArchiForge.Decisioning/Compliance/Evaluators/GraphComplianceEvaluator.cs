using ArchiForge.Decisioning.Compliance.Models;
using ArchiForge.KnowledgeGraph;
using ArchiForge.KnowledgeGraph.Models;

namespace ArchiForge.Decisioning.Compliance.Evaluators;

public class GraphComplianceEvaluator : IComplianceEvaluator
{
    public ComplianceEvaluationResult Evaluate(
        GraphSnapshot graphSnapshot,
        ComplianceRulePack rulePack)
    {
        ArgumentNullException.ThrowIfNull(graphSnapshot);
        ArgumentNullException.ThrowIfNull(rulePack);

        var result = new ComplianceEvaluationResult();
        var topologyNodes = graphSnapshot.GetNodesByType(GraphNodeTypes.TopologyResource);

        foreach (var rule in rulePack.Rules)
        {
            var resourcesInScope = topologyNodes
                .Where(x => string.Equals(
                    x.Category,
                    rule.AppliesToCategory,
                    StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (resourcesInScope.Count == 0)
                continue;

            var requiredNodes = graphSnapshot.Nodes
                .Where(x => string.Equals(
                    x.NodeType,
                    rule.RequiredNodeType,
                    StringComparison.OrdinalIgnoreCase))
                .ToList();

            var eligibleFromIds = requiredNodes
                .Select(x => x.NodeId)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var matchingEdges = graphSnapshot.Edges
                .Where(x =>
                    string.Equals(x.EdgeType, rule.RequiredEdgeType, StringComparison.OrdinalIgnoreCase) &&
                    eligibleFromIds.Contains(x.FromNodeId))
                .ToList();

            var coveredResourceIds = matchingEdges
                .Select(x => x.ToNodeId)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var uncoveredResources = resourcesInScope
                .Where(x => !coveredResourceIds.Contains(x.NodeId))
                .ToList();

            if (requiredNodes.Count == 0 || uncoveredResources.Count > 0)
            {
                result.Violations.Add(new ComplianceViolation
                {
                    RuleId = rule.RuleId,
                    ControlId = rule.ControlId,
                    ControlName = rule.ControlName,
                    AppliesToCategory = rule.AppliesToCategory,
                    Severity = rule.Severity,
                    Description = rule.Description,
                    AffectedNodeIds = uncoveredResources.Select(x => x.NodeId).ToList(),
                    AffectedResources = uncoveredResources.Select(x => x.Label).ToList()
                });
            }
        }

        return result;
    }
}
