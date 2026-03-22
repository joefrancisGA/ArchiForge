namespace ArchiForge.Api.Contracts;

public sealed class ArtifactDescriptorResponse
{
    public Guid ArtifactId { get; set; }
    public string ArtifactType { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string Format { get; set; } = null!;
    public DateTime CreatedUtc { get; set; }
    public string ContentHash { get; set; } = null!;
}
