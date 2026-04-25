using System.Text.Json;
using System.Text.Json.Serialization;

namespace ArchLucid.Provenance;

public static class ProvenanceGraphSerializer
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
        Converters = { new JsonStringEnumConverter() }
    };

    public static string Serialize(DecisionProvenanceGraph graph)
    {
        return JsonSerializer.Serialize(graph, Options);
    }

    public static DecisionProvenanceGraph? Deserialize(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return null;

        try
        {
            return JsonSerializer.Deserialize<DecisionProvenanceGraph>(json, Options);
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException(
                "Provenance graph JSON is corrupt and cannot be deserialized.", ex);
        }
    }
}
