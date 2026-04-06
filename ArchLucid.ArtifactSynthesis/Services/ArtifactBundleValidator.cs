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

        List<string> duplicateTypes = bundle.Artifacts
            .GroupBy(x => x.ArtifactType, StringComparer.OrdinalIgnoreCase)
            .Where(x => x.Count() > 1)
            .Select(x => x.Key)
            .ToList();

        if (duplicateTypes.Count > 0)
            throw new InvalidOperationException(
                $"Duplicate artifact types found: {string.Join(", ", duplicateTypes)}");

        foreach (SynthesizedArtifact artifact in bundle.Artifacts)
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
