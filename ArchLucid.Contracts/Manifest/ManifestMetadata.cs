namespace ArchiForge.Contracts.Manifest;

/// <summary>
/// Versioning and provenance metadata for a <see cref="GoldenManifest"/>.
/// </summary>
public sealed class ManifestMetadata
{
    /// <summary>
    /// Monotonically increasing version string for this manifest snapshot (e.g. <c>v1</c>, <c>v2</c>).
    /// Defaults to <c>v1</c> for new manifests.
    /// </summary>
    public string ManifestVersion { get; set; } = "v1";

    /// <summary>
    /// Version string of the manifest this snapshot was derived from, or
    /// <see langword="null"/> for the initial version.
    /// </summary>
    public string? ParentManifestVersion { get; set; }

    /// <summary>Human-readable description of what changed between this and the parent version.</summary>
    public string ChangeDescription { get; set; } = string.Empty;

    /// <summary>Identifiers of decision traces that contributed to this manifest version.</summary>
    public List<string> DecisionTraceIds { get; set; } = [];

    /// <summary>UTC timestamp when this manifest snapshot was committed.</summary>
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
}
