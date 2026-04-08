using ArchLucid.ArtifactSynthesis.Models;
using ArchLucid.Persistence.Serialization;

namespace ArchLucid.Persistence.ArtifactBundles;

/// <summary>Deserializes <c>TraceJson</c>; relational tables override list fields on the trace after load.</summary>
internal static class ArtifactBundleTraceJsonReader
{
    internal static SynthesisTrace DeserializeTraceBase(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return new SynthesisTrace();

        return JsonEntitySerializer.Deserialize<SynthesisTrace>(json);
    }
}
