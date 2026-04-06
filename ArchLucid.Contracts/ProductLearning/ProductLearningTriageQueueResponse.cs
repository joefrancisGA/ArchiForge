namespace ArchiForge.Contracts.ProductLearning;

/// <summary>Deterministic triage queue slice for operator review.</summary>
public sealed class ProductLearningTriageQueueResponse
{
    public DateTime GeneratedUtc { get; init; }
    public IReadOnlyList<TriageQueueItem> Items { get; init; } = [];
}
