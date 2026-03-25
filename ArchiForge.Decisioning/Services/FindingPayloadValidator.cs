using ArchiForge.Decisioning.Findings.Factories;
using ArchiForge.Decisioning.Findings.Payloads;
using ArchiForge.Decisioning.Interfaces;
using ArchiForge.Decisioning.Models;

namespace ArchiForge.Decisioning.Services;

public class FindingPayloadValidator : IFindingPayloadValidator
{
    private const string FindingTypeRequirementFinding = "RequirementFinding";
    private const string FindingTypeTopologyGap = "TopologyGap";
    private const string FindingTypeSecurityControlFinding = "SecurityControlFinding";
    private const string FindingTypeCostConstraintFinding = "CostConstraintFinding";
    private const string FindingTypePolicyApplicabilityFinding = "PolicyApplicabilityFinding";
    private const string FindingTypeTopologyCoverageFinding = "TopologyCoverageFinding";
    private const string FindingTypeSecurityCoverageFinding = "SecurityCoverageFinding";
    private const string FindingTypePolicyCoverageFinding = "PolicyCoverageFinding";
    private const string FindingTypeRequirementCoverageFinding = "RequirementCoverageFinding";
    private const string FindingTypeComplianceFinding = "ComplianceFinding";

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

        if (finding.FindingType.Equals(FindingTypeRequirementFinding, StringComparison.OrdinalIgnoreCase))
        {
            _ = FindingPayloadConverter.ToRequirementPayload(finding) ?? throw new InvalidOperationException("RequirementFinding payload is invalid.");
        }

        if (finding.FindingType.Equals(FindingTypeTopologyGap, StringComparison.OrdinalIgnoreCase))
        {
            _ = FindingPayloadConverter.ToTopologyGapPayload(finding) ?? throw new InvalidOperationException("TopologyGap payload is invalid.");
        }

        if (finding.FindingType.Equals(FindingTypeSecurityControlFinding, StringComparison.OrdinalIgnoreCase))
        {
            SecurityControlFindingPayload? payload = FindingPayloadConverter.ToSecurityControlPayload(finding);
            if (payload is null)
                throw new InvalidOperationException("SecurityControlFinding payload is invalid.");
        }

        if (finding.FindingType.Equals(FindingTypeCostConstraintFinding, StringComparison.OrdinalIgnoreCase))
        {
            CostConstraintFindingPayload? payload = FindingPayloadConverter.ToCostConstraintPayload(finding);
            if (payload is null)
                throw new InvalidOperationException("CostConstraintFinding payload is invalid.");
        }

        if (finding.FindingType.Equals(FindingTypePolicyApplicabilityFinding, StringComparison.OrdinalIgnoreCase))
        {
            _ = FindingPayloadConverter.ToPolicyApplicabilityPayload(finding) ?? throw new InvalidOperationException("PolicyApplicabilityFinding payload is invalid.");
        }

        if (finding.FindingType.Equals(FindingTypeTopologyCoverageFinding, StringComparison.OrdinalIgnoreCase))
        {
            _ = FindingPayloadConverter.ToTopologyCoveragePayload(finding) ?? throw new InvalidOperationException("TopologyCoverageFinding payload is invalid.");
        }

        if (finding.FindingType.Equals(FindingTypeSecurityCoverageFinding, StringComparison.OrdinalIgnoreCase))
        {
            _ = FindingPayloadConverter.ToSecurityCoveragePayload(finding) ?? throw new InvalidOperationException("SecurityCoverageFinding payload is invalid.");
        }

        if (finding.FindingType.Equals(FindingTypePolicyCoverageFinding, StringComparison.OrdinalIgnoreCase))
        {
            _ = FindingPayloadConverter.ToPolicyCoveragePayload(finding) ?? throw new InvalidOperationException("PolicyCoverageFinding payload is invalid.");
        }

        if (finding.FindingType.Equals(FindingTypeRequirementCoverageFinding, StringComparison.OrdinalIgnoreCase))
        {
            RequirementCoverageFindingPayload? payload = FindingPayloadConverter.ToRequirementCoveragePayload(finding);
            if (payload is null)
                throw new InvalidOperationException("RequirementCoverageFinding payload is invalid.");
        }

        if (!finding.FindingType.Equals(FindingTypeComplianceFinding, StringComparison.OrdinalIgnoreCase)) return;

        {
            ComplianceFindingPayload? payload = FindingPayloadConverter.ToCompliancePayload(finding);
            if (payload is null)
                throw new InvalidOperationException("ComplianceFinding payload is invalid.");
        }
    }
}

