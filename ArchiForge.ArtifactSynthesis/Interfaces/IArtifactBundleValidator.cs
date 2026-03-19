using ArchiForge.ArtifactSynthesis.Models;

namespace ArchiForge.ArtifactSynthesis.Interfaces;

public interface IArtifactBundleValidator
{
    void Validate(ArtifactBundle bundle);
}
