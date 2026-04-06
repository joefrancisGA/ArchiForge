namespace ArchiForge.Application.Analysis;

/// <summary>
/// Outcome of a drift analysis comparing a stored comparison payload against a freshly regenerated one.
/// </summary>
public sealed class DriftAnalysisResult
{
    /// <summary><c>true</c> when at least one field-level difference was found.</summary>
    public bool DriftDetected { get; set; }

    /// <summary>Individual field-level differences detected during the comparison.</summary>
    public List<DriftItem> Items { get; set; } = [];

    /// <summary>Human-readable summary (e.g. "3 drift differences detected." or "No drift detected.").</summary>
    public string Summary { get; set; } = string.Empty;
}

/// <summary>
/// A single field-level difference found during drift analysis.
/// </summary>
public sealed class DriftItem
{
    /// <summary>Broad category of change (e.g. <c>ValueChange</c>, <c>Added</c>, <c>Removed</c>, <c>TypeChange</c>, <c>ArrayLength</c>).</summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>JSON path to the differing field (e.g. <c>$.AgentDeltas[0].LeftConfidence</c>).</summary>
    public string Path { get; set; } = string.Empty;

    /// <summary>Serialized value from the stored payload; <c>null</c> when the field was added.</summary>
    public string? StoredValue { get; set; }

    /// <summary>Serialized value from the regenerated payload; <c>null</c> when the field was removed.</summary>
    public string? RegeneratedValue { get; set; }

    /// <summary>Human-readable description of the change.</summary>
    public string Description { get; set; } = string.Empty;
}
