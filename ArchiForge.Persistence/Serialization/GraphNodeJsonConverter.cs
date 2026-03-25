using System.Text.Json;
using System.Text.Json.Serialization;

using ArchiForge.KnowledgeGraph.Models;

namespace ArchiForge.Persistence.Serialization;

/// <summary>
/// Tolerates older or alternate JSON property names when reading <see cref="GraphNode"/> rows from storage
/// (e.g. <c>id</c>/<c>nodeId</c>, <c>type</c>/<c>nodeType</c>, <c>name</c>/<c>label</c>).
/// </summary>
internal sealed class GraphNodeJsonConverter : JsonConverter<GraphNode>
{
    public override GraphNode Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using JsonDocument doc = JsonDocument.ParseValue(ref reader);
        JsonElement root = doc.RootElement;
        if (root.ValueKind != JsonValueKind.Object)
            throw new JsonException("Expected JSON object for GraphNode.");

        return new GraphNode
        {
            NodeId = ReadFirstString(root, "nodeId", "id") ?? "",
            NodeType = ReadFirstString(root, "nodeType", "type") ?? "",
            Label = ReadFirstString(root, "label", "name") ?? "",
            Category = ReadFirstString(root, "category"),
            SourceType = ReadFirstString(root, "sourceType"),
            SourceId = ReadFirstString(root, "sourceId"),
            Properties = ReadProperties(root, options)
        };
    }

    public override void Write(Utf8JsonWriter writer, GraphNode value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteString("nodeId", value.NodeId);
        writer.WriteString("nodeType", value.NodeType);
        writer.WriteString("label", value.Label);
        if (value.Category is null)
            writer.WriteNull("category");
        else
            writer.WriteString("category", value.Category);
        if (value.SourceType is null)
            writer.WriteNull("sourceType");
        else
            writer.WriteString("sourceType", value.SourceType);
        if (value.SourceId is null)
            writer.WriteNull("sourceId");
        else
            writer.WriteString("sourceId", value.SourceId);
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

        try
        {
#pragma warning disable IDE0028 // Simplify collection initialization
            return JsonSerializer.Deserialize<Dictionary<string, string>>(propsEl.GetRawText(), options)
                   ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
#pragma warning restore IDE0028 // Simplify collection initialization
        }
        catch (JsonException)
        {
#pragma warning disable IDE0028 // Simplify collection initialization
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
#pragma warning restore IDE0028 // Simplify collection initialization
        }
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
