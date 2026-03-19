namespace ArchiForge.ArtifactSynthesis.Models;

public class CostSummaryArtifactModel
{
    public decimal? MaxMonthlyCost { get; set; }
    public List<string> Risks { get; set; } = new();
    public List<string> Notes { get; set; } = new();
}
