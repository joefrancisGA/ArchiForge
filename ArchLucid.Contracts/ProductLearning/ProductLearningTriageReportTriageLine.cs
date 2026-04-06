namespace ArchiForge.Contracts.ProductLearning;

/// <summary>One triage queue preview row for discussion / export (no raw signal payloads).</summary>
public sealed class ProductLearningTriageReportTriageLine
{
    public int Rank { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Severity { get; init; } = string.Empty;
    public string DetailSummary { get; init; } = string.Empty;
    public string? SuggestedNextStep { get; init; }
}
