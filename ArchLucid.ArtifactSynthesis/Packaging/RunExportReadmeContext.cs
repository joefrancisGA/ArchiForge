namespace ArchLucid.ArtifactSynthesis.Packaging;

/// <summary>Optional human-oriented fields appended to <c>README.txt</c> inside run export ZIPs.</summary>
public sealed record RunExportReadmeContext
{
    /// <summary>Manifest display name from metadata when present.</summary>
    public string? ManifestDisplayName { get; init; }

    /// <summary>Golden manifest content hash (same as <c>manifest.json</c> payload).</summary>
    public string? ManifestHash { get; init; }

    /// <summary>Rule set id and version, e.g. <c>ruleset 1.0</c>.</summary>
    public string? RuleSetLabel { get; init; }
}
