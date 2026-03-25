using System.Text.Json;
using System.Text.Json.Serialization;

using ArchiForge.Decisioning.Models;

namespace ArchiForge.Decisioning.Findings.Serialization;

/// <summary>
/// Serializes <see cref="Finding.Payload"/> as a typed JSON object; on read, rehydrates using <see cref="FindingPayloadRegistry"/>.
/// </summary>
public sealed class FindingJsonConverter : JsonConverter<Finding>
{
    public override Finding Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using JsonDocument doc = JsonDocument.ParseValue(ref reader);
        JsonElement root = doc.RootElement;
        Finding finding = new()
        {
            FindingSchemaVersion = root.TryGetProperty("findingSchemaVersion", out JsonElement fsv) && fsv.TryGetInt32(out int v) ? v : 0,
            FindingId = root.GetProperty("findingId").GetString() ?? Guid.NewGuid().ToString("N"),
            FindingType = root.GetProperty("findingType").GetString() ?? "",
            Category = root.TryGetProperty("category", out JsonElement cat) ? cat.GetString() ?? "" : "",
            EngineType = root.GetProperty("engineType").GetString() ?? "",
            Severity = root.TryGetProperty("severity", out JsonElement sev) && Enum.TryParse<FindingSeverity>(sev.GetString(), true, out FindingSeverity se)
                ? se
                : FindingSeverity.Info,
            Title = root.GetProperty("title").GetString() ?? "",
            Rationale = root.GetProperty("rationale").GetString() ?? "",
            RelatedNodeIds = ReadStringList(root, "relatedNodeIds"),
            RecommendedActions = ReadStringList(root, "recommendedActions"),
            Properties = ReadStringDict(root, "properties"),
            PayloadType = root.TryGetProperty("payloadType", out JsonElement pt) ? pt.GetString() : null,
        };

        finding.Trace = ReadTrace(root, options, finding);

        if (!root.TryGetProperty("payload", out JsonElement payloadEl) || payloadEl.ValueKind == JsonValueKind.Null)
            return finding;

        string? typeName = finding.PayloadType;
        Type? payloadType = FindingPayloadRegistry.ResolvePayloadType(typeName);
        try
        {
            finding.Payload = payloadType is not null
                ? JsonSerializer.Deserialize(payloadEl.GetRawText(), payloadType, options)
                : payloadEl.Clone();
        }
        catch (JsonException ex)
        {
            throw new JsonException(
                $"Failed to deserialize payload of type '{typeName}' for finding '{finding.FindingId}'.", ex);
        }

        return finding;
    }

    public override void Write(Utf8JsonWriter writer, Finding value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteNumber("findingSchemaVersion", value.FindingSchemaVersion);
        writer.WriteString("findingId", value.FindingId);
        writer.WriteString("findingType", value.FindingType);
        writer.WriteString("category", value.Category);
        writer.WriteString("engineType", value.EngineType);
        writer.WriteString("severity", value.Severity.ToString());
        writer.WriteString("title", value.Title);
        writer.WriteString("rationale", value.Rationale);
        writer.WritePropertyName("relatedNodeIds");
        JsonSerializer.Serialize(writer, value.RelatedNodeIds, options);
        writer.WritePropertyName("recommendedActions");
        JsonSerializer.Serialize(writer, value.RecommendedActions, options);
        writer.WritePropertyName("properties");
        JsonSerializer.Serialize(writer, value.Properties, options);
        if (value.PayloadType is not null)
            writer.WriteString("payloadType", value.PayloadType);
        else
            writer.WriteNull("payloadType");
        writer.WritePropertyName("payload");
        if (value.Payload is null)
            writer.WriteNullValue();
        else
            JsonSerializer.Serialize(writer, value.Payload, value.Payload.GetType(), options);
        writer.WritePropertyName("trace");
        JsonSerializer.Serialize(writer, value.Trace, options);
        writer.WriteEndObject();
    }

    /// <summary>
    /// Deserializes the <c>trace</c> property from <paramref name="root"/>.
    /// When deserialization fails the corrupt JSON is noted in <paramref name="finding"/>
    /// <c>Properties["_traceDeserializationWarning"]</c> so downstream consumers
    /// can detect data loss without silently discarding the error.
    /// </summary>
    private static ExplainabilityTrace ReadTrace(JsonElement root, JsonSerializerOptions options, Finding finding)
    {
        if (!root.TryGetProperty("trace", out JsonElement tr))
            return new ExplainabilityTrace();
        try
        {
            return JsonSerializer.Deserialize<ExplainabilityTrace>(tr.GetRawText(), options) ?? new ExplainabilityTrace();
        }
        catch (JsonException ex)
        {
            finding.Properties["_traceDeserializationWarning"] =
                $"Trace JSON could not be deserialized and was replaced with an empty trace. Error: {ex.Message}";
            return new ExplainabilityTrace();
        }
    }

    private static List<string> ReadStringList(JsonElement root, string name)
    {
        if (!root.TryGetProperty(name, out JsonElement el) || el.ValueKind != JsonValueKind.Array)
            return [];
        return el.EnumerateArray().Select(e => e.GetString() ?? "").Where(s => s.Length > 0).ToList();
    }

    private static Dictionary<string, string> ReadStringDict(JsonElement root, string name)
    {
        if (!root.TryGetProperty(name, out JsonElement el) || el.ValueKind != JsonValueKind.Object)
            return new Dictionary<string, string>();
        Dictionary<string, string> d = new(StringComparer.OrdinalIgnoreCase);
        foreach (JsonProperty p in el.EnumerateObject())
            d[p.Name] = p.Value.GetString() ?? "";
        return d;
    }
}
