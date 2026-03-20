using System.Text.Json;
using ArchiForge.Decisioning.Findings.Payloads;
using ArchiForge.Decisioning.Models;

namespace ArchiForge.Decisioning.Findings.Factories;

public static class FindingPayloadConverter
{
    public static T? ConvertPayload<T>(Finding finding)
    {
        if (finding.Payload is null)
            return default;

        if (finding.Payload is T typed)
            return typed;

        if (finding.Payload is JsonElement jsonElement)
        {
            return jsonElement.Deserialize<T>(new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }

        var json = JsonSerializer.Serialize(finding.Payload);
        return JsonSerializer.Deserialize<T>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
    }

    public static RequirementFindingPayload? ToRequirementPayload(Finding finding)
        => ConvertPayload<RequirementFindingPayload>(finding);

    public static TopologyGapFindingPayload? ToTopologyGapPayload(Finding finding)
        => ConvertPayload<TopologyGapFindingPayload>(finding);

    public static SecurityControlFindingPayload? ToSecurityControlPayload(Finding finding)
        => ConvertPayload<SecurityControlFindingPayload>(finding);

    public static CostConstraintFindingPayload? ToCostConstraintPayload(Finding finding)
        => ConvertPayload<CostConstraintFindingPayload>(finding);

    public static PolicyApplicabilityFindingPayload? ToPolicyApplicabilityPayload(Finding finding)
        => ConvertPayload<PolicyApplicabilityFindingPayload>(finding);

    public static TopologyCoverageFindingPayload? ToTopologyCoveragePayload(Finding finding)
        => ConvertPayload<TopologyCoverageFindingPayload>(finding);

    public static SecurityCoverageFindingPayload? ToSecurityCoveragePayload(Finding finding)
        => ConvertPayload<SecurityCoverageFindingPayload>(finding);

    public static PolicyCoverageFindingPayload? ToPolicyCoveragePayload(Finding finding)
        => ConvertPayload<PolicyCoverageFindingPayload>(finding);

    public static RequirementCoverageFindingPayload? ToRequirementCoveragePayload(Finding finding)
        => ConvertPayload<RequirementCoverageFindingPayload>(finding);
}

