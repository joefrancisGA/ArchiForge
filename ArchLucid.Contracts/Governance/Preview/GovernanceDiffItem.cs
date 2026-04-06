namespace ArchiForge.Contracts.Governance.Preview;

/// <summary>
/// A single manifest-level difference surfaced by a governance preview or environment comparison.
/// </summary>
public sealed class GovernanceDiffItem
{
    /// <summary>Manifest element key that changed (e.g. service name, control id).</summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>Type of change: <c>Added</c>, <c>Removed</c>, or <c>Modified</c>.</summary>
    public string ChangeType { get; set; } = string.Empty;

    /// <summary>Value in the currently active (base) manifest, or <see langword="null"/> when the item is new.</summary>
    public string? CurrentValue { get; set; }

    /// <summary>Value in the preview (candidate) manifest, or <see langword="null"/> when the item is removed.</summary>
    public string? PreviewValue { get; set; }
}
