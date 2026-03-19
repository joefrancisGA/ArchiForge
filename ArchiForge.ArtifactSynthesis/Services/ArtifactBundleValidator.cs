using ArchiForge.ArtifactSynthesis.Interfaces;
using ArchiForge.ArtifactSynthesis.Models;

namespace ArchiForge.ArtifactSynthesis.Services;

public class ArtifactBundleValidator : IArtifactBundleValidator
{
    public void Validate(ArtifactBundle bundle)
    {
        if (bundle.BundleId == Guid.Empty)
            throw new InvalidOperationException("BundleId is required.");

        if (bundle.ManifestId == Guid.Empty)
            throw new InvalidOperationException("ManifestId is required.");

        if (bundle.Artifacts.Count == 0)
            throw new InvalidOperationException("At least one artifact is required.");

        foreach (var artifact in bundle.Artifacts)
        {
            if (string.IsNullOrWhiteSpace(artifact.ArtifactType))
                throw new InvalidOperationException("ArtifactType is required.");

            if (string.IsNullOrWhiteSpace(artifact.Content))
                throw new InvalidOperationException("Artifact content is required.");

            if (string.IsNullOrWhiteSpace(artifact.ContentHash))
                throw new InvalidOperationException("Artifact content hash is required.");
        }
    }
}
