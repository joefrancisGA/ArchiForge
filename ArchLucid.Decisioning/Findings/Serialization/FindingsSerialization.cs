using System.Text.Json;
using System.Text.Json.Serialization;

using ArchLucid.Decisioning.Models;

namespace ArchLucid.Decisioning.Findings.Serialization;

/// <summary>
///     Round-trip serialization for findings snapshots with typed payload rehydration.
/// </summary>
public static class FindingsSerialization
{
    private static readonly JsonSerializerOptions Options = CreateOptions();

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

    public static string SerializeSnapshot(FindingsSnapshot snapshot)
    {
        return JsonSerializer.Serialize(snapshot, Options);
    }

    public static FindingsSnapshot DeserializeSnapshot(string json)
    {
        FindingsSnapshot s = JsonSerializer.Deserialize<FindingsSnapshot>(json, Options) ?? new FindingsSnapshot();
        FindingsSnapshotMigrator.Apply(s);
        return s;
    }
}
