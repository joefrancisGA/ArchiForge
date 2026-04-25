using ArchLucid.ContextIngestion.Models;
using ArchLucid.Persistence.Serialization;

namespace ArchLucid.Persistence.ContextSnapshots;

/// <summary>Deserializes legacy JSON columns on <c>dbo.ContextSnapshots</c> when relational child rows are absent.</summary>
internal static class ContextSnapshotLegacyJsonReader
{
    internal static List<CanonicalObject> DeserializeCanonicalObjects(string? json)
    {
        return string.IsNullOrWhiteSpace(json) ? [] : JsonEntitySerializer.Deserialize<List<CanonicalObject>>(json);
    }

    internal static List<string> DeserializeStringList(string? json)
    {
        return string.IsNullOrWhiteSpace(json) ? [] : JsonEntitySerializer.Deserialize<List<string>>(json);
    }

    internal static Dictionary<string, string> DeserializeSourceHashes(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return new Dictionary<string, string>(StringComparer.Ordinal);

        Dictionary<string, string> parsed = JsonEntitySerializer.Deserialize<Dictionary<string, string>>(json);
        Dictionary<string, string> ordinal = new(StringComparer.Ordinal);

        foreach (KeyValuePair<string, string> pair in parsed)
            ordinal[pair.Key] = pair.Value;

        return ordinal;
    }
}
