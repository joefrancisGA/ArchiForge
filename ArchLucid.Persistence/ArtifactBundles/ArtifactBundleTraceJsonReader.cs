using ArchLucid.ArtifactSynthesis.Models;
using ArchLucid.Persistence.Serialization;

namespace ArchLucid.Persistence.ArtifactBundles;

/// <summary>Deserializes <c>TraceJson</c>; relational tables override list fields on the trace after load.</summary>
internal static class ArtifactBundleTraceJsonReader
{
    internal static SynthesisTrace DeserializeTraceBase(string? json)
    {
        return string.IsNullOrWhiteSpace(json)
            ? new SynthesisTrace()
            : JsonEntitySerializer.Deserialize<SynthesisTrace>(json);
    }
}
