namespace ArchLucid.ArtifactSynthesis.Models;

/// <summary>Relational artifact row without <c>Content</c> NVARCHAR(MAX) for list paging.</summary>
public sealed record ArtifactMetadataRow(
    int SortOrder,
    Guid ArtifactId,
    Guid RunId,
    Guid ManifestId,
    DateTime CreatedUtc,
    string ArtifactType,
    string Name,
    string Format,
    string ContentHash,
    string? ContentBlobUri);

/// <summary>Keyset bundle artifact page (<c>SortOrder ASC</c>, <c>ArtifactId ASC</c>).</summary>
public sealed record ArtifactBundleArtifactMetadataPage(IReadOnlyList<ArtifactMetadataRow> Items, bool HasMore);
