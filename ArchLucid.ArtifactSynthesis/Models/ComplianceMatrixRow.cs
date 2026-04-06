using System.Diagnostics.CodeAnalysis;

namespace ArchiForge.ArtifactSynthesis.Models;

[ExcludeFromCodeCoverage(Justification = "Artifact synthesis row DTO; no logic.")]
public class ComplianceMatrixRow
{
    public string ControlId { get; set; } = null!;
    public string ControlName { get; set; } = null!;
    public string AppliesToCategory { get; set; } = null!;
    public string Status { get; set; } = null!;
    public string Notes { get; set; } = null!;
}
