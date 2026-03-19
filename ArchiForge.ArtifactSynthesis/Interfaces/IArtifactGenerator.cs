using ArchiForge.ArtifactSynthesis.Models;
using ArchiForge.Decisioning.Models;

namespace ArchiForge.ArtifactSynthesis.Interfaces;

public interface IArtifactGenerator
{
    string ArtifactType { get; }

    Task<SynthesizedArtifact> GenerateAsync(
        GoldenManifest manifest,
        CancellationToken ct);
}
