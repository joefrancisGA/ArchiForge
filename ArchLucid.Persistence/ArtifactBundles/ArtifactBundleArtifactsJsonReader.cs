using ArchLucid.ArtifactSynthesis.Models;
using ArchLucid.Persistence.Serialization;

namespace ArchLucid.Persistence.ArtifactBundles;

/// <summary>Deserializes <c>ArtifactsJson</c> when relational <c>ArtifactBundleArtifacts</c> rows are absent.</summary>
internal static class ArtifactBundleArtifactsJsonReader
{
    internal static List<SynthesizedArtifact> DeserializeArtifacts(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return [];

        return JsonEntitySerializer.Deserialize<List<SynthesizedArtifact>>(json);
    }
}
