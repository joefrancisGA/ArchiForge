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

        var securityNodes = graphSnapshot.Nodes
            .Where(n => n.NodeType == "SecurityBaseline")
            .ToList();

        foreach (var node in securityNodes)
        {
            node.Properties.TryGetValue("controlId", out var controlId);
            node.Properties.TryGetValue("status", out var status);

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
                Rationale = "A security baseline node was found in the graph and should influence resolved architecture decisions.",
                RelatedNodeIds = [node.NodeId],
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
                    GraphNodeIdsExamined = [node.NodeId],
                    DecisionsTaken = ["Converted security graph node into a security finding."]
                }
            });
        }

        return Task.FromResult<IReadOnlyList<Finding>>(findings);
    }
}

