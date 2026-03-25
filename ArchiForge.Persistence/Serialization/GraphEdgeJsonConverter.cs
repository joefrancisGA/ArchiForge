using System.Text.Json;
using System.Text.Json.Serialization;

using ArchiForge.KnowledgeGraph.Models;

namespace ArchiForge.Persistence.Serialization;

/// <summary>
/// Tolerates alternate property names when reading <see cref="GraphEdge"/> rows (e.g. <c>id</c> for <c>edgeId</c>).
/// </summary>
internal sealed class GraphEdgeJsonConverter : JsonConverter<GraphEdge>
{
    public override GraphEdge Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using JsonDocument doc = JsonDocument.ParseValue(ref reader);
        JsonElement root = doc.RootElement;
        if (root.ValueKind != JsonValueKind.Object)
            throw new JsonException("Expected JSON object for GraphEdge.");

        return new GraphEdge
        {
            EdgeId = ReadFirstString(root, "edgeId", "id") ?? "",
            FromNodeId = ReadFirstString(root, "fromNodeId", "from", "source") ?? "",
            ToNodeId = ReadFirstString(root, "toNodeId", "to", "target") ?? "",
            EdgeType = ReadFirstString(root, "edgeType", "type", "relation") ?? "",
            Label = ReadFirstString(root, "label"),
            Properties = ReadProperties(root, options)
        };
    }

    public override void Write(Utf8JsonWriter writer, GraphEdge value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteString("edgeId", value.EdgeId);
        writer.WriteString("fromNodeId", value.FromNodeId);
        writer.WriteString("toNodeId", value.ToNodeId);
        writer.WriteString("edgeType", value.EdgeType);
        if (value.Label is null)
            writer.WriteNull("label");
        else
            writer.WriteString("label", value.Label);
        writer.WritePropertyName("properties");
        JsonSerializer.Serialize(writer, value.Properties, options);
        writer.WriteEndObject();
    }

    private static Dictionary<string, string> ReadProperties(JsonElement root, JsonSerializerOptions options)
    {
        if (!TryGetIgnoreCase(root, "properties", out JsonElement propsEl) || propsEl.ValueKind != JsonValueKind.Object)
#pragma warning disable IDE0028 // Simplify collection initialization
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
#pragma warning restore IDE0028 // Simplify collection initialization

#pragma warning disable IDE0028 // Simplify collection initialization
        return JsonSerializer.Deserialize<Dictionary<string, string>>(propsEl.GetRawText(), options)
               ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
#pragma warning restore IDE0028 // Simplify collection initialization
    }

    private static string? ReadFirstString(JsonElement root, params string[] names)
    {
        foreach (string name in names)
        {
            if (TryGetIgnoreCase(root, name, out JsonElement el) && el.ValueKind == JsonValueKind.String)
                return el.GetString();
        }

        return null;
    }

    private static bool TryGetIgnoreCase(JsonElement obj, string name, out JsonElement value)
    {
        foreach (JsonProperty p in obj.EnumerateObject().Where(p => p.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
        {
            value = p.Value;
            return true;
        }

        value = default;
        return false;
    }
}
