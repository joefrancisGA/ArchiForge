using ArchLucid.Contracts.Agents;
using ArchLucid.Decisioning.Findings.Payloads;
using ArchLucid.Decisioning.Models;
using ArchLucid.KnowledgeGraph.Models;

using ExplainabilityMarkers = ArchLucid.Decisioning.Findings.ExplainabilityTraceMarkers;

namespace ArchLucid.Decisioning.Findings.Factories;

public static class FindingFactory
{
    public static Finding CreateRequirementFinding(
        string engineType,
        string title,
        string rationale,
        string requirementName,
        string requirementText,
        bool isMandatory,
        IEnumerable<string>? relatedNodeIds = null)
    {
        return new Finding
        {
            FindingSchemaVersion = FindingsSchema.CurrentFindingVersion,
            FindingType = "RequirementFinding",
            Category = "Requirement",
            EngineType = engineType,
            Severity = FindingSeverity.Info,
            Title = title,
            Rationale = rationale,
            RelatedNodeIds = relatedNodeIds?.ToList() ?? [],
            PayloadType = nameof(RequirementFindingPayload),
            Payload = new RequirementFindingPayload { RequirementName = requirementName, RequirementText = requirementText, IsMandatory = isMandatory }
        };
    }

    public static Finding CreateTopologyGapFinding(
        string engineType,
        string title,
        string rationale,
        string gapCode,
        string description,
        string impact,
        FindingSeverity severity = FindingSeverity.Warning,
        IEnumerable<string>? relatedNodeIds = null)
    {
        return new Finding
        {
            FindingSchemaVersion = FindingsSchema.CurrentFindingVersion,
            FindingType = "TopologyGap",
            Category = "Topology",
            EngineType = engineType,
            Severity = severity,
            Title = title,
            Rationale = rationale,
            RelatedNodeIds = relatedNodeIds?.ToList() ?? [],
            PayloadType = nameof(TopologyGapFindingPayload),
            Payload =
                new TopologyGapFindingPayload { GapCode = gapCode, Description = description, Impact = impact },
            Trace = new ExplainabilityTrace
            {
                GraphNodeIdsExamined = relatedNodeIds?.ToList() ?? [],
                RulesApplied = [$"topology-gap-{gapCode}"],
                DecisionsTaken = [$"Detected topology gap: {description}"],
                AlternativePathsConsidered = [ExplainabilityMarkers.RuleBasedDeterministicSinglePathNote]
            }
        };
    }

    public static Finding CreatePolicyApplicabilityFinding(
        string engineType,
        GraphNode policyNode,
        string? policyReference,
        IReadOnlyList<string> applicableTopologyNodeIds,
        IReadOnlyList<string> graphNodeIdsExamined)
    {
        return new Finding
        {
            FindingSchemaVersion = FindingsSchema.CurrentFindingVersion,
            FindingType = "PolicyApplicabilityFinding",
            Category = "Policy",
            EngineType = engineType,
            Severity = FindingSeverity.Info,
            Title = $"Policy applicability: {policyNode.Label}",
            Rationale =
                "The knowledge graph links this policy control to one or more topology resources via APPLIES_TO edges.",
            RelatedNodeIds = graphNodeIdsExamined.Distinct(StringComparer.OrdinalIgnoreCase).ToList(),
            PayloadType = nameof(PolicyApplicabilityFindingPayload),
            Payload = new PolicyApplicabilityFindingPayload
            {
                PolicyName = policyNode.Label,
                PolicyReference = policyReference,
                ApplicableTopologyResourceCount = applicableTopologyNodeIds.Count,
                ApplicableTopologyNodeIds = applicableTopologyNodeIds.ToList()
            },
            Trace = new ExplainabilityTrace
            {
                GraphNodeIdsExamined = graphNodeIdsExamined.Distinct(StringComparer.OrdinalIgnoreCase).ToList(),
                RulesApplied = ["policy-applicability-mapping"],
                DecisionsTaken =
                    ["Interpreted APPLIES_TO edges as policy applicability to topology resources."],
                AlternativePathsConsidered = [ExplainabilityMarkers.RuleBasedDeterministicSinglePathNote],
                Notes = [$"Applicable topology targets: {applicableTopologyNodeIds.Count}"]
            }
        };
    }

    public static Finding CreatePolicyApplicabilityGapFinding(
        string engineType,
        GraphNode policyNode,
        string? policyReference,
        string gapRationale)
    {
        return new Finding
        {
            FindingSchemaVersion = FindingsSchema.CurrentFindingVersion,
            FindingType = "PolicyApplicabilityFinding",
            Category = "Policy",
            EngineType = engineType,
            Severity = FindingSeverity.Warning,
            Title = $"Policy has no topology applicability: {policyNode.Label}",
            Rationale = gapRationale,
            RelatedNodeIds = [policyNode.NodeId],
            PayloadType = nameof(PolicyApplicabilityFindingPayload),
            Payload = new PolicyApplicabilityFindingPayload
            {
                PolicyName = policyNode.Label, PolicyReference = policyReference, ApplicableTopologyResourceCount = 0, ApplicableTopologyNodeIds = []
            },
            Trace = new ExplainabilityTrace
            {
                GraphNodeIdsExamined = [policyNode.NodeId],
                RulesApplied = ["policy-applicability-gap"],
                DecisionsTaken = ["No APPLIES_TO edges from this policy to TopologyResource nodes were found."],
                AlternativePathsConsidered = [ExplainabilityMarkers.RuleBasedDeterministicSinglePathNote],
                Notes = [$"Policy: {policyNode.Label}"]
            }
        };
    }

    /// <summary>
    ///     Maps an LLM <see cref="ArchitectureFinding" /> plus agent/execution metadata into a persisted-shaped
    ///     <see cref="Finding" />.
    /// </summary>
    public static Finding CreateFromAgentArchitectureFinding(
        ArchitectureFinding finding,
        AgentResult agentResult,
        AgentExecutionTrace? trace = null)
    {
        ArgumentNullException.ThrowIfNull(finding);
        ArgumentNullException.ThrowIfNull(agentResult);

        string? traceKey = trace?.TraceId;
        string agentExecutionTraceId;

        if (!string.IsNullOrEmpty(traceKey))
        {
            agentExecutionTraceId = traceKey.Length > 32 ? traceKey[..32] : traceKey;
        }
        else
        {
            string fid = finding.FindingId;
            agentExecutionTraceId = fid.Length > 32 ? fid[..32] : fid;
        }

        List<string> notes = finding.EvidenceRefs.ConvertAll(static r => $"evidence:{r}");

        return new Finding
        {
            FindingSchemaVersion = FindingsSchema.CurrentFindingVersion,
            FindingId = finding.FindingId,
            FindingType = $"AgentArchitectureFinding-{agentResult.AgentType}",
            Category =
                string.IsNullOrWhiteSpace(finding.Category) ? agentResult.AgentType.ToString() : finding.Category,
            EngineType = agentResult.AgentType.ToString(),
            Severity = finding.Severity,
            Title = finding.Message.Length > 500 ? finding.Message[..500] : finding.Message,
            Rationale = finding.Message,
            RelatedNodeIds = [],
            ConfidenceScore = finding.ConfidenceScore ?? agentResult.Confidence,
            EvaluationConfidenceScore = finding.EvaluationConfidenceScore,
            ConfidenceLevel = finding.ConfidenceLevel,
            AgentExecutionTraceId = agentExecutionTraceId,
            ModelDeploymentName = trace?.ModelDeploymentName,
            ModelVersion = trace?.ModelVersion,
            PromptTemplateId = trace?.PromptTemplateId,
            PromptTemplateVersion = trace?.PromptTemplateVersion,
            Trace = new ExplainabilityTrace
            {
                SourceAgentExecutionTraceId = traceKey,
                Notes = notes,
                AlternativePathsConsidered = [ExplainabilityMarkers.RuleBasedDeterministicSinglePathNote],
            },
        };
    }
}
