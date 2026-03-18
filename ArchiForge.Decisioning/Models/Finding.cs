namespace ArchiForge.Decisioning.Models;

public class Finding
{
    /// <summary>Schema version of this finding record (increment when envelope or payload contracts change).</summary>
    public int FindingSchemaVersion { get; set; } = FindingsSchema.CurrentFindingVersion;

    public string FindingId { get; set; } = Guid.NewGuid().ToString("N");
    public string FindingType { get; set; } = default!;
    public string Category { get; set; } = default!;
    public string EngineType { get; set; } = default!;
    public FindingSeverity Severity { get; set; }

    public string Title { get; set; } = default!;
    public string Rationale { get; set; } = default!;

    public List<string> RelatedNodeIds { get; set; } = new();
    public List<string> RecommendedActions { get; set; } = new();

    public Dictionary<string, string> Properties { get; set; } = new();

    public object? Payload { get; set; }
    public string? PayloadType { get; set; }

    public ExplainabilityTrace Trace { get; set; } = new();
}

