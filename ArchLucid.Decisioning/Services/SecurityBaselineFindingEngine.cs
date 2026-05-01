using ArchLucid.Decisioning.Findings.Payloads;
using ArchLucid.Decisioning.Interfaces;
using ArchLucid.Decisioning.Models;
using ArchLucid.KnowledgeGraph.Models;

namespace ArchLucid.Decisioning.Services;

public class SecurityBaselineFindingEngine : IFindingEngine
{
    public string EngineType => "security-baseline";
    public string Category => "Security";

    public Task<IReadOnlyList<Finding>> AnalyzeAsync(
        GraphSnapshot graphSnapshot,
        CancellationToken ct)
    {
        List<Finding> findings = [];

        IReadOnlyList<GraphNode> securityNodes = graphSnapshot.GetNodesByType("SecurityBaseline");

        foreach (GraphNode node in securityNodes)
        {
            node.Properties.TryGetValue("controlId", out string? controlId);
            node.Properties.TryGetValue("status", out string? status);

            List<string> protectedIds = graphSnapshot
                .GetOutgoingTargets(node.NodeId, "PROTECTS")
                .Select(n => n.NodeId)
                .ToList();

            List<string> relatedNodeIds = [node.NodeId];

            foreach (string id in protectedIds.Where(id =>
                         !relatedNodeIds.Contains(id, StringComparer.OrdinalIgnoreCase)))

                relatedNodeIds.Add(id);

            List<string> examined = [.. relatedNodeIds];

            findings.Add(new Finding
            {
                FindingSchemaVersion = FindingsSchema.CurrentFindingVersion,
                FindingType = "SecurityControlFinding",
                Category = "Security",
                EngineType = EngineType,
                Severity = string.Equals(status, "missing", StringComparison.OrdinalIgnoreCase)
                    ? FindingSeverity.Error
                    : FindingSeverity.Info,
                Title = $"Security baseline control: {node.Label}",
                Rationale = protectedIds.Count > 0
                    ? "A security baseline node was found; PROTECTS edges associate it with topology resources that should inherit control scope."
                    : "A security baseline node was found in the graph and should influence resolved architecture decisions.",
                RelatedNodeIds = relatedNodeIds,
                PayloadType = nameof(SecurityControlFindingPayload),
                Payload = new SecurityControlFindingPayload
                {
                    ControlId = controlId ?? string.Empty,
                    ControlName = node.Label,
                    Status = status ?? "unknown",
                    Impact = string.Equals(status, "missing", StringComparison.OrdinalIgnoreCase)
                        ? "Security control is missing and should be enforced."
                        : "Security control is present."
                },
                Trace = new ExplainabilityTrace
                {
                    GraphNodeIdsExamined = examined,
                    RulesApplied = ["security-baseline-coverage"],
                    DecisionsTaken =
                    [
                        protectedIds.Count > 0
                            ? "Included topology targets linked via PROTECTS edges in explainability scope."
                            : "Converted security graph node into a security finding."
                    ],
                    AlternativePathsConsidered = string.Equals(status, "missing", StringComparison.OrdinalIgnoreCase)
                        ?
                        [
                            "Document compensating controls and accept residual risk for this control gap.",
                            "Defer enforcement with a tracked exception tied to a remediation date."
                        ]
                        :
                        [
                            "Tighten scope: require explicit PROTECTS edges for every in-scope resource.",
                            "Treat as informational only when the control is inherited from a platform baseline."
                        ],
                    Notes = [$"PROTECTS edge count: {protectedIds.Count}"]
                }
            });
        }

        return Task.FromResult<IReadOnlyList<Finding>>(findings);
    }
}
