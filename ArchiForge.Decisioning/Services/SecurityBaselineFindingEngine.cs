using ArchiForge.Decisioning.Findings.Payloads;
using ArchiForge.Decisioning.Interfaces;
using ArchiForge.Decisioning.Models;
using ArchiForge.KnowledgeGraph.Models;

namespace ArchiForge.Decisioning.Services;

public class SecurityBaselineFindingEngine : IFindingEngine
{
    public string EngineType => "security-baseline";
    public string Category => "Security";

    public Task<IReadOnlyList<Finding>> AnalyzeAsync(
        GraphSnapshot graphSnapshot,
        CancellationToken ct)
    {
        var findings = new List<Finding>();

        var securityNodes = graphSnapshot.GetNodesByType("SecurityBaseline");

        foreach (var node in securityNodes)
        {
            node.Properties.TryGetValue("controlId", out var controlId);
            node.Properties.TryGetValue("status", out var status);

            var protectedIds = graphSnapshot
                .GetOutgoingTargets(node.NodeId, "PROTECTS")
                .Select(n => n.NodeId)
                .ToList();

            var relatedNodeIds = new List<string> { node.NodeId };
            foreach (var id in protectedIds)
            {
                if (!relatedNodeIds.Contains(id, StringComparer.OrdinalIgnoreCase))
                    relatedNodeIds.Add(id);
            }

            var examined = new List<string>(relatedNodeIds);

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
                    DecisionsTaken =
                    [
                        protectedIds.Count > 0
                            ? "Included topology targets linked via PROTECTS edges in explainability scope."
                            : "Converted security graph node into a security finding."
                    ]
                }
            });
        }

        return Task.FromResult<IReadOnlyList<Finding>>(findings);
    }
}
