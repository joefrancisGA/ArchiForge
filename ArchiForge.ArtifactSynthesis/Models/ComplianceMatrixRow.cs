namespace ArchiForge.ArtifactSynthesis.Models;

public class ComplianceMatrixRow
{
    public string ControlId { get; set; } = default!;
    public string ControlName { get; set; } = default!;
    public string AppliesToCategory { get; set; } = default!;
    public string Status { get; set; } = default!;
    public string Notes { get; set; } = default!;
}
