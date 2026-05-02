using ArchLucid.Decisioning.Compliance.Models;
using ArchLucid.KnowledgeGraph;
using ArchLucid.KnowledgeGraph.Models;

namespace ArchLucid.Decisioning.Compliance.Evaluators;

public class GraphComplianceEvaluator : IComplianceEvaluator
{
    public ComplianceEvaluationResult Evaluate(
        GraphSnapshot graphSnapshot,
        ComplianceRulePack rulePack)
    {
        ArgumentNullException.ThrowIfNull(graphSnapshot);
        ArgumentNullException.ThrowIfNull(rulePack);

        ComplianceEvaluationResult result = new();
        IReadOnlyList<GraphNode> topologyNodes = graphSnapshot.GetNodesByType(GraphNodeTypes.TopologyResource);

        foreach (ComplianceRule rule in rulePack.Rules)
        {
            List<GraphNode> resourcesInScope = topologyNodes
                .Where(x => string.Equals(
                    x.Category,
                    rule.AppliesToCategory,
                    StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (resourcesInScope.Count == 0)
                continue;

            List<GraphNode> requiredNodes = graphSnapshot.Nodes
                .Where(x => string.Equals(
                    x.NodeType,
                    rule.RequiredNodeType,
                    StringComparison.OrdinalIgnoreCase))
                .ToList();

            HashSet<string> eligibleFromIds = requiredNodes
                .Select(x => x.NodeId)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            List<GraphEdge> matchingEdges = graphSnapshot.Edges
                .Where(x =>
                    string.Equals(x.EdgeType, rule.RequiredEdgeType, StringComparison.OrdinalIgnoreCase) &&
                    eligibleFromIds.Contains(x.FromNodeId) &&
                    x.Weight >= GraphEdgeDecisioningThresholds.MinWeightForSemanticLink)
                .ToList();

            HashSet<string> coveredResourceIds = matchingEdges
                .Select(x => x.ToNodeId)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            List<GraphNode> uncoveredResources = resourcesInScope
                .Where(x => !coveredResourceIds.Contains(x.NodeId))
                .ToList();

            if (requiredNodes.Count == 0 || uncoveredResources.Count > 0)

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

        return result;
    }
}
