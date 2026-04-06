namespace ArchiForge.Contracts.DecisionTraces;

/// <summary>
/// Coordinator run event (options evaluated, merges applied, etc.); carried on <see cref="RunEventTrace.RunEvent"/>.
/// </summary>
public sealed class RunEventTracePayload
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
