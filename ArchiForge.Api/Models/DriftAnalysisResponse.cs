namespace ArchiForge.Api.Models;

public sealed class DriftAnalysisResponse
{
    public bool DriftDetected { get; set; }

    public string Summary { get; set; } = string.Empty;

    public List<DriftItemResponse> Items { get; set; } = new();
}
