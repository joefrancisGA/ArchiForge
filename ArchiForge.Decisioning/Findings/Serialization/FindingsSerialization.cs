using System.Text.Json;
using System.Text.Json.Serialization;

using ArchiForge.Decisioning.Models;

namespace ArchiForge.Decisioning.Findings.Serialization;

/// <summary>
/// Round-trip serialization for findings snapshots with typed payload rehydration.
/// </summary>
public static class FindingsSerialization
{
    public static JsonSerializerOptions CreateOptions()
    {
        JsonSerializerOptions o = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };
        o.Converters.Add(new FindingJsonConverter());
        return o;
    }

    private static readonly JsonSerializerOptions Options = CreateOptions();

    public static string SerializeSnapshot(FindingsSnapshot snapshot)
        => JsonSerializer.Serialize(snapshot, Options);

    public static FindingsSnapshot DeserializeSnapshot(string json)
    {
        FindingsSnapshot s = JsonSerializer.Deserialize<FindingsSnapshot>(json, Options) ?? new FindingsSnapshot();
        FindingsSnapshotMigrator.Apply(s);
        return s;
    }
}
