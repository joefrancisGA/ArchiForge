using ArchiForge.ArtifactSynthesis.Models;
using ArchiForge.Decisioning.Models;

namespace ArchiForge.ArtifactSynthesis.Interfaces;

public interface IArtifactSynthesisService
{
    Task<ArtifactBundle> SynthesizeAsync(
        GoldenManifest manifest,
        CancellationToken ct);
}
