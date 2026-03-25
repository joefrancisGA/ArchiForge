namespace ArchiForge.Contracts.Governance.Preview;

/// <summary>
/// Result of a governance promotion preview: compares the candidate manifest against the manifest currently
/// active in the target environment and surfaces the diff items and any advisory notes.
/// </summary>
public sealed class GovernancePreviewResult
{
    /// <summary>Environment slot evaluated (e.g. <c>dev</c>, <c>test</c>, <c>prod</c>).</summary>
    public string Environment { get; set; } = string.Empty;

    /// <summary>Run id currently active in the environment, or <see langword="null"/> when no run is promoted.</summary>
    public string? CurrentRunId { get; set; }

    /// <summary>Manifest version currently active, or <see langword="null"/> when none is promoted.</summary>
    public string? CurrentManifestVersion { get; set; }

    /// <summary>Run id of the candidate being previewed for promotion.</summary>
    public string PreviewRunId { get; set; } = string.Empty;

    /// <summary>Manifest version of the candidate being previewed.</summary>
    public string PreviewManifestVersion { get; set; } = string.Empty;

    /// <summary>Diff items between the currently active manifest and the preview candidate.</summary>
    public List<GovernanceDiffItem> Differences { get; set; } = [];

    /// <summary>Advisory notes generated during the preview (e.g. first-promotion or diff-only notes).</summary>
    public List<string> Notes { get; set; } = [];
}
