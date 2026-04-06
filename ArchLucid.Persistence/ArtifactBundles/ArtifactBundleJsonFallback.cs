using ArchiForge.ArtifactSynthesis.Models;
using ArchiForge.Persistence.Serialization;

namespace ArchiForge.Persistence.ArtifactBundles;

/// <summary>JSON column reads for artifact bundles when relational slices are absent or for trace base hydration.</summary>
internal static class ArtifactBundleJsonFallback
{
    /// <remarks>TODO: remove JSON fallback after relational migration complete (artifact list from <c>ArtifactsJson</c>).</remarks>
    internal static List<SynthesizedArtifact> DeserializeArtifacts(string json)
    {
        return JsonEntitySerializer.Deserialize<List<SynthesizedArtifact>>(json);
    }

    /// <summary>Deserializes the trace payload from <c>TraceJson</c>; relational tables may override list fields.</summary>
    internal static SynthesisTrace DeserializeTraceBase(string json)
    {
        return JsonEntitySerializer.Deserialize<SynthesisTrace>(json);
    }
}
