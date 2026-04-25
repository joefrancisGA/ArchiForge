using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace ArchLucid.Persistence.Serialization;

public static class JsonEntitySerializer
{
    /// <summary>
    ///     Shared options (read-only) for entity JSON round-trip; used by distributed hot-path cache and persistence
    ///     serializers.
    /// </summary>
    public static JsonSerializerOptions EntityJsonOptions
    {
        get;
    } = CreateEntityJsonOptions();

    private static JsonSerializerOptions CreateEntityJsonOptions()
    {
        JsonSerializerOptions o = new()
        {
            PropertyNameCaseInsensitive = true,
            WriteIndented = false,
            TypeInfoResolver = new DefaultJsonTypeInfoResolver(),
            Converters = { new GraphNodeJsonConverter(), new GraphEdgeJsonConverter() }
        };

        o.MakeReadOnly();
        return o;
    }

    public static string Serialize<T>(T value)
    {
        return JsonSerializer.Serialize(value, EntityJsonOptions);
    }

    public static T Deserialize<T>(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            throw new InvalidOperationException("Cannot deserialize empty JSON.");

        T? value;
        try
        {
            value = JsonSerializer.Deserialize<T>(json, EntityJsonOptions);
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException($"JSON for {typeof(T).Name} is corrupt and cannot be deserialized.",
                ex);
        }

        return value ?? throw new InvalidOperationException($"Failed to deserialize {typeof(T).Name}.");
    }
}
