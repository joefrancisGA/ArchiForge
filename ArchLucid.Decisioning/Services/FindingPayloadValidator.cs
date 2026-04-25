using ArchLucid.Decisioning.Findings;
using ArchLucid.Decisioning.Findings.Factories;
using ArchLucid.Decisioning.Findings.Payloads;
using ArchLucid.Decisioning.Interfaces;
using ArchLucid.Decisioning.Models;

namespace ArchLucid.Decisioning.Services;

public class FindingPayloadValidator : IFindingPayloadValidator
{
    public void Validate(Finding finding)
    {
        ArgumentNullException.ThrowIfNull(finding);

        if (string.IsNullOrWhiteSpace(finding.FindingType))
            throw new InvalidOperationException("FindingType is required.");

        if (string.IsNullOrWhiteSpace(finding.Category))
            throw new InvalidOperationException("Category is required.");

        if (string.IsNullOrWhiteSpace(finding.EngineType))
            throw new InvalidOperationException("EngineType is required.");

        if (finding.Payload is null && !string.IsNullOrWhiteSpace(finding.PayloadType))
            throw new InvalidOperationException("PayloadType was set but Payload is null.");

        if (finding.FindingType.Equals(FindingTypes.RequirementFinding, StringComparison.OrdinalIgnoreCase))

            _ = FindingPayloadConverter.ToRequirementPayload(finding) ??
                throw new InvalidOperationException("RequirementFinding payload is invalid.");


        if (finding.FindingType.Equals(FindingTypes.TopologyGap, StringComparison.OrdinalIgnoreCase))

            _ = FindingPayloadConverter.ToTopologyGapPayload(finding) ??
                throw new InvalidOperationException("TopologyGap payload is invalid.");


        if (finding.FindingType.Equals(FindingTypes.SecurityControlFinding, StringComparison.OrdinalIgnoreCase))
        {
            SecurityControlFindingPayload? payload = FindingPayloadConverter.ToSecurityControlPayload(finding);

            if (payload is null)
                throw new InvalidOperationException("SecurityControlFinding payload is invalid.");
        }

        if (finding.FindingType.Equals(FindingTypes.CostConstraintFinding, StringComparison.OrdinalIgnoreCase))
        {
            CostConstraintFindingPayload? payload = FindingPayloadConverter.ToCostConstraintPayload(finding);

            if (payload is null)
                throw new InvalidOperationException("CostConstraintFinding payload is invalid.");
        }

        if (finding.FindingType.Equals(FindingTypes.PolicyApplicabilityFinding, StringComparison.OrdinalIgnoreCase))

            _ = FindingPayloadConverter.ToPolicyApplicabilityPayload(finding) ??
                throw new InvalidOperationException("PolicyApplicabilityFinding payload is invalid.");


        if (finding.FindingType.Equals(FindingTypes.TopologyCoverageFinding, StringComparison.OrdinalIgnoreCase))

            _ = FindingPayloadConverter.ToTopologyCoveragePayload(finding) ??
                throw new InvalidOperationException("TopologyCoverageFinding payload is invalid.");


        if (finding.FindingType.Equals(FindingTypes.SecurityCoverageFinding, StringComparison.OrdinalIgnoreCase))

            _ = FindingPayloadConverter.ToSecurityCoveragePayload(finding) ??
                throw new InvalidOperationException("SecurityCoverageFinding payload is invalid.");


        if (finding.FindingType.Equals(FindingTypes.PolicyCoverageFinding, StringComparison.OrdinalIgnoreCase))

            _ = FindingPayloadConverter.ToPolicyCoveragePayload(finding) ??
                throw new InvalidOperationException("PolicyCoverageFinding payload is invalid.");


        if (finding.FindingType.Equals(FindingTypes.RequirementCoverageFinding, StringComparison.OrdinalIgnoreCase))
        {
            RequirementCoverageFindingPayload? payload = FindingPayloadConverter.ToRequirementCoveragePayload(finding);

            if (payload is null)
                throw new InvalidOperationException("RequirementCoverageFinding payload is invalid.");
        }

        if (!finding.FindingType.Equals(FindingTypes.ComplianceFinding, StringComparison.OrdinalIgnoreCase))
            return;

        ComplianceFindingPayload? compliancePayload = FindingPayloadConverter.ToCompliancePayload(finding);

        if (compliancePayload is null)
            throw new InvalidOperationException("ComplianceFinding payload is invalid.");
    }
}
