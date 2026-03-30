using ArchiForge.ArtifactSynthesis.Models;
using ArchiForge.ArtifactSynthesis.Packaging;

namespace ArchiForge.Api.Contracts;

/// <summary>
/// JSON descriptor for a synthesized artifact (listing, metadata GET, and operator review UIs).
/// </summary>
public sealed class ArtifactDescriptorResponse
{
    public Guid ArtifactId { get; set; }
    public string ArtifactType { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string Format { get; set; } = null!;
    public DateTime CreatedUtc { get; set; }
    public string ContentHash { get; set; } = null!;

    public static ArtifactDescriptorResponse From(ArtifactDescriptor descriptor) =>
        new()
        {
            ArtifactId = descriptor.ArtifactId,
            ArtifactType = descriptor.ArtifactType,
            Name = descriptor.Name,
            Format = descriptor.Format,
            CreatedUtc = descriptor.CreatedUtc,
            ContentHash = descriptor.ContentHash,
        };

    public static ArtifactDescriptorResponse From(SynthesizedArtifact artifact) =>
        new()
        {
            ArtifactId = artifact.ArtifactId,
            ArtifactType = artifact.ArtifactType,
            Name = artifact.Name,
            Format = artifact.Format,
            CreatedUtc = artifact.CreatedUtc,
            ContentHash = artifact.ContentHash,
        };
}
