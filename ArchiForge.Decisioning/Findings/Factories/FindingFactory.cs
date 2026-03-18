using ArchiForge.Decisioning.Findings.Payloads;
using ArchiForge.Decisioning.Models;

namespace ArchiForge.Decisioning.Findings.Factories;

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
            RelatedNodeIds = relatedNodeIds?.ToList() ?? new List<string>(),
            PayloadType = nameof(RequirementFindingPayload),
            Payload = new RequirementFindingPayload
            {
                RequirementName = requirementName,
                RequirementText = requirementText,
                IsMandatory = isMandatory
            }
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
            RelatedNodeIds = relatedNodeIds?.ToList() ?? new List<string>(),
            PayloadType = nameof(TopologyGapFindingPayload),
            Payload = new TopologyGapFindingPayload
            {
                GapCode = gapCode,
                Description = description,
                Impact = impact
            }
        };
    }
}

