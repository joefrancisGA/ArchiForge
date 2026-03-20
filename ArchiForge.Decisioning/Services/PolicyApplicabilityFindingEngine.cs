using ArchiForge.Decisioning.Findings.Factories;
using ArchiForge.Decisioning.Interfaces;
using ArchiForge.Decisioning.Models;
using ArchiForge.KnowledgeGraph.Models;

namespace ArchiForge.Decisioning.Services;

/// <summary>
/// Surfaces <c>APPLIES_TO</c> edges from <c>PolicyControl</c> graph nodes to topology resources.
/// </summary>
public class PolicyApplicabilityFindingEngine : IFindingEngine
{
    public string EngineType => "policy-applicability";
    public string Category => "Policy";

    public Task<IReadOnlyList<Finding>> AnalyzeAsync(
        GraphSnapshot graphSnapshot,
        CancellationToken ct)
    {
        var findings = new List<Finding>();
        var policyNodes = graphSnapshot.GetNodesByType("PolicyControl");
        var topologyCount = graphSnapshot.GetNodesByType("TopologyResource").Count;

        foreach (var policy in policyNodes)
        {
            var targets = graphSnapshot
                .GetOutgoingTargets(policy.NodeId, "APPLIES_TO")
                .Where(n => string.Equals(n.NodeType, "TopologyResource", StringComparison.OrdinalIgnoreCase))
                .ToList();

            policy.Properties.TryGetValue("reference", out var reference);
            policy.Properties.TryGetValue("policyId", out var policyId);
            var policyRef = reference ?? policyId;

            if (topologyCount > 0 && targets.Count == 0)
            {
                findings.Add(FindingFactory.CreatePolicyApplicabilityGapFinding(
                    EngineType,
                    policy,
                    policyRef,
                    gapRationale: "Policy is present but has no APPLIES_TO links to topology resources in this graph."));
                continue;
            }

            if (targets.Count == 0)
                continue;

            var targetIds = targets.Select(t => t.NodeId).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            var examined = new List<string> { policy.NodeId };
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
