namespace ArchiForge.Contracts.Metadata;

public sealed class DecisionTrace
{
    public string TraceId { get; set; } = Guid.NewGuid().ToString("N");

    public string RunId { get; set; } = string.Empty;

    public string EventType { get; set; } = string.Empty;

    public string EventDescription { get; set; } = string.Empty;

    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

    public Dictionary<string, string> Metadata { get; set; } = new();
}
