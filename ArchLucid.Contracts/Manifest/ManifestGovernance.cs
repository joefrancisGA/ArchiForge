namespace ArchiForge.Contracts.Manifest;

/// <summary>
/// Governance metadata attached to a <see cref="GoldenManifest"/>, capturing compliance
/// requirements, policy constraints, required controls, and risk/cost classifications
/// for the resolved architecture.
/// </summary>
public sealed class ManifestGovernance
{
    /// <summary>Compliance framework or regulatory tags applicable to this architecture (e.g. <c>ISO27001</c>, <c>SOC2</c>).</summary>
    public List<string> ComplianceTags { get; set; } = [];

    /// <summary>Policy constraint strings that the architecture must satisfy.</summary>
    public List<string> PolicyConstraints { get; set; } = [];

    /// <summary>Cross-cutting security and operational controls required across all components.</summary>
    public List<string> RequiredControls { get; set; } = [];

    /// <summary>Risk tier for this architecture (e.g. <c>Low</c>, <c>Moderate</c>, <c>High</c>). Defaults to <c>Moderate</c>.</summary>
    public string RiskClassification { get; set; } = "Moderate";

    /// <summary>Cost tier for this architecture (e.g. <c>Low</c>, <c>Moderate</c>, <c>High</c>). Defaults to <c>Moderate</c>.</summary>
    public string CostClassification { get; set; } = "Moderate";
}
