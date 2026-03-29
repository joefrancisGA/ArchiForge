namespace ArchiForge.Contracts.Metadata;

/// <summary>
/// Append-only log entry emitted by the decision engine during a run (options evaluated, merges applied, etc.).
/// </summary>
/// <remarks>Persisted with the run and exposed in run detail for audit and analysis UIs.</remarks>
public sealed class DecisionTrace
{
    /// <summary>Unique row identifier.</summary>
    public string TraceId { get; set; } = Guid.NewGuid().ToString("N");

    /// <summary>Owning architecture run id.</summary>
    public string RunId { get; set; } = string.Empty;

    /// <summary>Machine-oriented event classifier (e.g. option selected, manifest merged).</summary>
    public string EventType { get; set; } = string.Empty;

    /// <summary>Human-readable description of what happened at this step.</summary>
    public string EventDescription { get; set; } = string.Empty;

    /// <summary>UTC timestamp when the event was recorded.</summary>
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

    /// <summary>Structured facets (ids, versions, scores) attached to the event.</summary>
    public Dictionary<string, string> Metadata { get; set; } = [];
}
