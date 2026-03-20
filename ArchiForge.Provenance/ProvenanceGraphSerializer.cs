using System.Text.Json;
using System.Text.Json.Serialization;

namespace ArchiForge.Provenance;

public static class ProvenanceGraphSerializer
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
        Converters = { new JsonStringEnumConverter() }
    };

    public static string Serialize(DecisionProvenanceGraph graph) =>
        JsonSerializer.Serialize(graph, Options);

    public static DecisionProvenanceGraph? Deserialize(string json) =>
        string.IsNullOrWhiteSpace(json) ? null : JsonSerializer.Deserialize<DecisionProvenanceGraph>(json, Options);
}
