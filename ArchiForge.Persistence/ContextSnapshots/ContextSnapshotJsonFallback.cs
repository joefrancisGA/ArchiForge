using ArchiForge.ContextIngestion.Models;
using ArchiForge.Persistence.Serialization;

namespace ArchiForge.Persistence.ContextSnapshots;

/// <summary>
/// Legacy JSON column deserialization for <see cref="ContextSnapshot"/> when relational child rows are absent.
/// </summary>
internal static class ContextSnapshotJsonFallback
{
    public static List<CanonicalObject> DeserializeCanonicalObjects(string? json)
    {
        if (string.IsNullOrWhiteSpace(json) || string.Equals(json, "[]", StringComparison.Ordinal))
            return [];

        return JsonEntitySerializer.Deserialize<List<CanonicalObject>>(json);
    }

    public static List<string> DeserializeStringList(string? json)
    {
        if (string.IsNullOrWhiteSpace(json) || string.Equals(json, "[]", StringComparison.Ordinal))
            return [];

        return JsonEntitySerializer.Deserialize<List<string>>(json);
    }

    public static Dictionary<string, string> DeserializeStringDictionary(string? json)
    {
        if (string.IsNullOrWhiteSpace(json) || string.Equals(json, "{}", StringComparison.Ordinal))
            return new Dictionary<string, string>(StringComparer.Ordinal);

        return JsonEntitySerializer.Deserialize<Dictionary<string, string>>(json);
    }
}
