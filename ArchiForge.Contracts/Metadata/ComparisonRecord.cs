namespace ArchiForge.Contracts.Metadata;

/// <summary>
/// Stored output of a comparison between two runs, manifests, or exports (payload is format-specific JSON plus optional markdown summary).
/// </summary>
public sealed class ComparisonRecord
{
    /// <summary>Primary key.</summary>
    public string ComparisonRecordId { get; set; } = Guid.NewGuid().ToString("N");

    /// <summary>Classifier for the comparison pipeline (e.g. manifest diff, export diff).</summary>
    public string ComparisonType { get; set; } = string.Empty;

    /// <summary>Left-hand run id when the comparison is run-scoped.</summary>
    public string? LeftRunId
    {
        get; set;
    }

    /// <summary>Right-hand run id when the comparison is run-scoped.</summary>
    public string? RightRunId
    {
        get; set;
    }

    /// <summary>Left manifest version when comparing versions without full run pairing.</summary>
    public string? LeftManifestVersion
    {
        get; set;
    }

    /// <summary>Right manifest version when comparing versions without full run pairing.</summary>
    public string? RightManifestVersion
    {
        get; set;
    }

    /// <summary>Left export record id when the diff is anchored on exported artifacts.</summary>
    public string? LeftExportRecordId
    {
        get; set;
    }

    /// <summary>Right export record id when the diff is anchored on exported artifacts.</summary>
    public string? RightExportRecordId
    {
        get; set;
    }

    /// <summary>Serialization of <see cref="PayloadJson"/> (typically <c>json</c>).</summary>
    public string Format { get; set; } = "json";

    /// <summary>Optional human-readable synopsis of the diff.</summary>
    public string? SummaryMarkdown
    {
        get; set;
    }

    /// <summary>Structured comparison payload (shape depends on <see cref="ComparisonType"/>).</summary>
    public string PayloadJson { get; set; } = string.Empty;

    /// <summary>Operator notes captured when the record was created or updated.</summary>
    public string? Notes
    {
        get; set;
    }

    /// <summary>UTC creation time.</summary>
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

    /// <summary>Optional short label (e.g. release-1.2, incident-42).</summary>
    public string? Label
    {
        get; set;
    }

    /// <summary>Optional tags for filtering and grouping.</summary>
    public List<string> Tags { get; set; } = [];
}

