using ArchiForge.Decisioning.Manifest.Sections;
using ArchiForge.Decisioning.Models;
using ArchiForge.Persistence.Serialization;

namespace ArchiForge.Persistence.GoldenManifests;

/// <summary>Phase-1 JSON column reads when relational slice rows are absent.</summary>
/// <remarks>TODO: remove JSON fallback after relational migration complete.</remarks>
internal static class GoldenManifestJsonFallback
{
    internal static List<string> DeserializeStringList(string json)
    {
        return JsonEntitySerializer.Deserialize<List<string>>(json);
    }

    internal static ManifestProvenance DeserializeProvenance(string json)
    {
        return JsonEntitySerializer.Deserialize<ManifestProvenance>(json);
    }

    internal static List<ResolvedArchitectureDecision> DeserializeDecisions(string json)
    {
        return JsonEntitySerializer.Deserialize<List<ResolvedArchitectureDecision>>(json);
    }
}
