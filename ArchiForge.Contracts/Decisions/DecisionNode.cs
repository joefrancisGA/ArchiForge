namespace ArchiForge.Contracts.Decisions;

public sealed class DecisionNode
{
    public string DecisionId { get; set; } = Guid.NewGuid().ToString("N");

    public string RunId { get; set; } = string.Empty;

    public string Topic { get; set; } = string.Empty;

    public List<DecisionOption> Options { get; set; } = [];

    public string? SelectedOptionId { get; set; }

    public string Rationale { get; set; } = string.Empty;

    public double Confidence { get; set; }

    public List<string> SupportingEvaluationIds { get; set; } = [];

    public List<string> OpposingEvaluationIds { get; set; } = [];

    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
}

