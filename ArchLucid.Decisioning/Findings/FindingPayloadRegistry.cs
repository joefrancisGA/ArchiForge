using ArchiForge.Decisioning.Findings.Payloads;

namespace ArchiForge.Decisioning.Findings;

public static class FindingPayloadRegistry
{
    private static readonly Dictionary<string, Type> ByPayloadTypeName = new(StringComparer.OrdinalIgnoreCase)
    {
        [nameof(RequirementFindingPayload)] = typeof(RequirementFindingPayload),
        [nameof(TopologyGapFindingPayload)] = typeof(TopologyGapFindingPayload),
        [nameof(SecurityControlFindingPayload)] = typeof(SecurityControlFindingPayload),
        [nameof(CostConstraintFindingPayload)] = typeof(CostConstraintFindingPayload),
        [nameof(PolicyApplicabilityFindingPayload)] = typeof(PolicyApplicabilityFindingPayload),
        [nameof(TopologyCoverageFindingPayload)] = typeof(TopologyCoverageFindingPayload),
        [nameof(SecurityCoverageFindingPayload)] = typeof(SecurityCoverageFindingPayload),
        [nameof(PolicyCoverageFindingPayload)] = typeof(PolicyCoverageFindingPayload),
        [nameof(RequirementCoverageFindingPayload)] = typeof(RequirementCoverageFindingPayload),
        [nameof(ComplianceFindingPayload)] = typeof(ComplianceFindingPayload)
    };

    public static IReadOnlyDictionary<string, Type> RegisteredTypes => ByPayloadTypeName;

    public static Type? ResolvePayloadType(string? payloadTypeName)
    {
        return string.IsNullOrWhiteSpace(payloadTypeName) ? null : ByPayloadTypeName.GetValueOrDefault(payloadTypeName);
    }
}
