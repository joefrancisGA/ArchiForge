using System.Diagnostics.CodeAnalysis;

using ArchiForge.ArtifactSynthesis.Models;
using ArchiForge.ArtifactSynthesis.Packaging;

namespace ArchiForge.Api.Contracts;

/// <summary>
/// JSON descriptor for a synthesized artifact (listing, metadata GET, and operator review UIs).
/// </summary>
[ExcludeFromCodeCoverage(Justification = "API contract DTO; no business logic.")]
public sealed class ArtifactDescriptorResponse
{
    public Guid ArtifactId { get; set; }
    public string ArtifactType { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string Format { get; set; } = null!;
    public DateTime CreatedUtc { get; set; }
    public string ContentHash { get; set; } = null!;

    /// <summary>Golden manifest that produced this artifact (set on list and descriptor responses).</summary>
    public Guid? ManifestId { get; set; }

    /// <summary>Authority run when known from stored artifact rows (descriptor from synthesis).</summary>
    public Guid? RunId { get; set; }

    /// <summary>List projection: correlates each row with the manifest id from the route.</summary>
    public static ArtifactDescriptorResponse From(ArtifactDescriptor descriptor, Guid manifestId) =>
        new()
        {
            ArtifactId = descriptor.ArtifactId,
            ArtifactType = descriptor.ArtifactType,
            Name = descriptor.Name,
            Format = descriptor.Format,
            CreatedUtc = descriptor.CreatedUtc,
            ContentHash = descriptor.ContentHash,
            ManifestId = manifestId,
            RunId = null,
        };

    /// <summary>Single-artifact descriptor including scope ids from the synthesized record.</summary>
    public static ArtifactDescriptorResponse From(SynthesizedArtifact artifact) =>
        new()
        {
            ArtifactId = artifact.ArtifactId,
            ArtifactType = artifact.ArtifactType,
            Name = artifact.Name,
            Format = artifact.Format,
            CreatedUtc = artifact.CreatedUtc,
            ContentHash = artifact.ContentHash,
            ManifestId = artifact.ManifestId,
            RunId = artifact.RunId,
        };
}
