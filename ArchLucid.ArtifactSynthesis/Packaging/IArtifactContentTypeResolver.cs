using ArchiForge.ArtifactSynthesis.Models;

namespace ArchiForge.ArtifactSynthesis.Packaging;

public interface IArtifactContentTypeResolver
{
    string Resolve(SynthesizedArtifact artifact);
}
