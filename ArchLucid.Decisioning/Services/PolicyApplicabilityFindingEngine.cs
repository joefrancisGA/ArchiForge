using ArchLucid.Decisioning.Findings.Factories;
using ArchLucid.Decisioning.Interfaces;
using ArchLucid.Decisioning.Models;
using ArchLucid.KnowledgeGraph;
using ArchLucid.KnowledgeGraph.Models;

namespace ArchLucid.Decisioning.Services;

/// <summary>
///     Surfaces <c>APPLIES_TO</c> edges from <c>PolicyControl</c> graph nodes to topology resources.
/// </summary>
public class PolicyApplicabilityFindingEngine : IFindingEngine
{
    public string EngineType => "policy-applicability";
    public string Category => "Policy";

    public Task<IReadOnlyList<Finding>> AnalyzeAsync(
        GraphSnapshot graphSnapshot,
        CancellationToken ct)
    {
        List<Finding> findings = [];
        IReadOnlyList<GraphNode> policyNodes = graphSnapshot.GetNodesByType("PolicyControl");
        int topologyCount = graphSnapshot.GetNodesByType("TopologyResource").Count;

        foreach (GraphNode policy in policyNodes)
        {
            List<GraphNode> targets = graphSnapshot
                .GetOutgoingTargets(
                    policy.NodeId,
                    "APPLIES_TO",
                    GraphEdgeDecisioningThresholds.MinWeightForSemanticLink)
                .Where(n => string.Equals(n.NodeType, "TopologyResource", StringComparison.OrdinalIgnoreCase))
                .ToList();

            policy.Properties.TryGetValue("reference", out string? reference);
            policy.Properties.TryGetValue("policyId", out string? policyId);
            string? policyRef = reference ?? policyId;

            if (topologyCount > 0 && targets.Count == 0)
            {
                findings.Add(FindingFactory.CreatePolicyApplicabilityGapFinding(
                    EngineType,
                    policy,
                    policyRef,
                    "Policy is present but has no APPLIES_TO links to topology resources in this graph."));
                continue;
            }

            if (targets.Count == 0)
                continue;

            List<string> targetIds = targets.Select(t => t.NodeId).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            List<string> examined = [policy.NodeId];
            examined.AddRange(targetIds);

            findings.Add(FindingFactory.CreatePolicyApplicabilityFinding(
                EngineType,
                policy,
                policyRef,
                targetIds,
                examined));
        }

        return Task.FromResult<IReadOnlyList<Finding>>(findings);
    }
}
