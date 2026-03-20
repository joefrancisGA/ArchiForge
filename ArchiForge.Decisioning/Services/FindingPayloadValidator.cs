using ArchiForge.Decisioning.Findings.Factories;
using ArchiForge.Decisioning.Interfaces;
using ArchiForge.Decisioning.Models;

namespace ArchiForge.Decisioning.Services;

public class FindingPayloadValidator : IFindingPayloadValidator
{
    public void Validate(Finding finding)
    {
        if (string.IsNullOrWhiteSpace(finding.FindingType))
            throw new InvalidOperationException("FindingType is required.");

        if (string.IsNullOrWhiteSpace(finding.Category))
            throw new InvalidOperationException("Category is required.");

        if (string.IsNullOrWhiteSpace(finding.EngineType))
            throw new InvalidOperationException("EngineType is required.");

        if (finding.Payload is null && !string.IsNullOrWhiteSpace(finding.PayloadType))
            throw new InvalidOperationException("PayloadType was set but Payload is null.");

        if (finding.FindingType.Equals("RequirementFinding", StringComparison.OrdinalIgnoreCase))
        {
            var payload = FindingPayloadConverter.ToRequirementPayload(finding);
            if (payload is null)
                throw new InvalidOperationException("RequirementFinding payload is invalid.");
        }

        if (finding.FindingType.Equals("TopologyGap", StringComparison.OrdinalIgnoreCase))
        {
            var payload = FindingPayloadConverter.ToTopologyGapPayload(finding);
            if (payload is null)
                throw new InvalidOperationException("TopologyGap payload is invalid.");
        }

        if (finding.FindingType.Equals("SecurityControlFinding", StringComparison.OrdinalIgnoreCase))
        {
            var payload = FindingPayloadConverter.ToSecurityControlPayload(finding);
            if (payload is null)
                throw new InvalidOperationException("SecurityControlFinding payload is invalid.");
        }

        if (finding.FindingType.Equals("CostConstraintFinding", StringComparison.OrdinalIgnoreCase))
        {
            var payload = FindingPayloadConverter.ToCostConstraintPayload(finding);
            if (payload is null)
                throw new InvalidOperationException("CostConstraintFinding payload is invalid.");
        }

        if (finding.FindingType.Equals("PolicyApplicabilityFinding", StringComparison.OrdinalIgnoreCase))
        {
            var payload = FindingPayloadConverter.ToPolicyApplicabilityPayload(finding);
            if (payload is null)
                throw new InvalidOperationException("PolicyApplicabilityFinding payload is invalid.");
        }

        if (finding.FindingType.Equals("TopologyCoverageFinding", StringComparison.OrdinalIgnoreCase))
        {
            var payload = FindingPayloadConverter.ToTopologyCoveragePayload(finding);
            if (payload is null)
                throw new InvalidOperationException("TopologyCoverageFinding payload is invalid.");
        }

        if (finding.FindingType.Equals("SecurityCoverageFinding", StringComparison.OrdinalIgnoreCase))
        {
            var payload = FindingPayloadConverter.ToSecurityCoveragePayload(finding);
            if (payload is null)
                throw new InvalidOperationException("SecurityCoverageFinding payload is invalid.");
        }

        if (finding.FindingType.Equals("PolicyCoverageFinding", StringComparison.OrdinalIgnoreCase))
        {
            var payload = FindingPayloadConverter.ToPolicyCoveragePayload(finding);
            if (payload is null)
                throw new InvalidOperationException("PolicyCoverageFinding payload is invalid.");
        }

        if (finding.FindingType.Equals("RequirementCoverageFinding", StringComparison.OrdinalIgnoreCase))
        {
            var payload = FindingPayloadConverter.ToRequirementCoveragePayload(finding);
            if (payload is null)
                throw new InvalidOperationException("RequirementCoverageFinding payload is invalid.");
        }

        if (finding.FindingType.Equals("ComplianceFinding", StringComparison.OrdinalIgnoreCase))
        {
            var payload = FindingPayloadConverter.ToCompliancePayload(finding);
            if (payload is null)
                throw new InvalidOperationException("ComplianceFinding payload is invalid.");
        }
    }
}

