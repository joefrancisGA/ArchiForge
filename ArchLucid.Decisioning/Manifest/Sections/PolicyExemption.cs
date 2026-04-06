using System.Diagnostics.CodeAnalysis;

namespace ArchiForge.Decisioning.Manifest.Sections;

/// <summary>A policy control that has been explicitly exempted for this manifest.</summary>
[ExcludeFromCodeCoverage(Justification = "Manifest section DTO; no logic.")]
public class PolicyExemption
{
    /// <summary>Identifier of the exempted control (matches <see cref="PolicyControlItem.ControlId"/>).</summary>
    public string ControlId { get; set; } = string.Empty;

    /// <summary>Human-readable reason for the exemption.</summary>
    public string Justification { get; set; } = string.Empty;

    /// <summary>Optional expiry date for time-limited exemptions (UTC).</summary>
    public DateTime? ExpiresUtc { get; set; }
}
