namespace ArchiForge.Decisioning.Manifest.Sections;

/// <summary>
/// First-class policy data for a <see cref="ArchiForge.Decisioning.Models.GoldenManifest"/>.
/// Tracks resolved policy controls, violations, and exemptions rather than folding them
/// into <see cref="ComplianceSection"/> assumptions or warnings.
/// </summary>
public class PolicySection
{
    /// <summary>Policy controls that have been verified as satisfied for this manifest.</summary>
    public List<PolicyControlItem> SatisfiedControls { get; set; } = [];

    /// <summary>Policy controls that are required but not yet satisfied (violations).</summary>
    public List<PolicyControlItem> Violations { get; set; } = [];

    /// <summary>Policy controls that are explicitly exempted, with justification.</summary>
    public List<PolicyExemption> Exemptions { get; set; } = [];

    /// <summary>Free-text policy notes that do not map to a specific control.</summary>
    public List<string> Notes { get; set; } = [];
}
