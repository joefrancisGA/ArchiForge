namespace ArchiForge.Decisioning.Findings;

/// <summary>
/// Canonical <see cref="Models.Finding.FindingType"/> string constants used throughout
/// <see cref="Models.FindingsSnapshot.GetByType"/> calls and finding engines.
/// Using these constants rather than bare string literals gives compile-time safety
/// and prevents silent empty-result bugs from typos.
/// </summary>
public static class FindingTypes
{
    public const string RequirementFinding = "RequirementFinding";
    public const string TopologyGap = "TopologyGap";
    public const string SecurityControlFinding = "SecurityControlFinding";
    public const string ComplianceFinding = "ComplianceFinding";
    public const string CostConstraintFinding = "CostConstraintFinding";
    public const string PolicyApplicabilityFinding = "PolicyApplicabilityFinding";
    public const string TopologyCoverageFinding = "TopologyCoverageFinding";
    public const string SecurityCoverageFinding = "SecurityCoverageFinding";
    public const string PolicyCoverageFinding = "PolicyCoverageFinding";
    public const string RequirementCoverageFinding = "RequirementCoverageFinding";
}
