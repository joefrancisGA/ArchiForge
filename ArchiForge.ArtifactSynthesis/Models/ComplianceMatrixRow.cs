namespace ArchiForge.ArtifactSynthesis.Models;

public class ComplianceMatrixRow
{
    public string ControlId { get; set; } = null!;
    public string ControlName { get; set; } = null!;
    public string AppliesToCategory { get; set; } = null!;
    public string Status { get; set; } = null!;
    public string Notes { get; set; } = null!;
}
