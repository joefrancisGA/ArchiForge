using ArchiForge.ArtifactSynthesis.Models;

namespace ArchiForge.ArtifactSynthesis.Packaging;

public class ArtifactContentTypeResolver : IArtifactContentTypeResolver
{
    public string Resolve(SynthesizedArtifact artifact)
    {
        return artifact.Format.ToLowerInvariant() switch
        {
            "json" => "application/json",
            "markdown" => "text/markdown",
            _ => "text/plain"
        };
    }
}
