namespace ArchiForge.Decisioning.Models;

public class CostSection
{
    public decimal? MaxMonthlyCost { get; set; }
    public List<string> CostRisks { get; set; } = [];
    public List<string> Notes { get; set; } = [];
}

