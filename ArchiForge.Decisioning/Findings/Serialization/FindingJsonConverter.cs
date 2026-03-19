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
        using var doc = JsonDocument.ParseValue(ref reader);
        var root = doc.RootElement;
        var finding = new Finding
        {
            FindingSchemaVersion = root.TryGetProperty("findingSchemaVersion", out var fsv) && fsv.TryGetInt32(out var v) ? v : 0,
            FindingId = root.GetProperty("findingId").GetString() ?? Guid.NewGuid().ToString("N"),
            FindingType = root.GetProperty("findingType").GetString() ?? "",
            Category = root.TryGetProperty("category", out var cat) ? cat.GetString() ?? "" : "",
            EngineType = root.GetProperty("engineType").GetString() ?? "",
            Severity = root.TryGetProperty("severity", out var sev) && Enum.TryParse<FindingSeverity>(sev.GetString(), true, out var se)
                ? se
                : FindingSeverity.Info,
            Title = root.GetProperty("title").GetString() ?? "",
            Rationale = root.GetProperty("rationale").GetString() ?? "",
            RelatedNodeIds = ReadStringList(root, "relatedNodeIds"),
            RecommendedActions = ReadStringList(root, "recommendedActions"),
            Properties = ReadStringDict(root, "properties"),
            PayloadType = root.TryGetProperty("payloadType", out var pt) ? pt.GetString() : null,
            Trace = root.TryGetProperty("trace", out var tr)
                ? JsonSerializer.Deserialize<ExplainabilityTrace>(tr.GetRawText(), options) ?? new ExplainabilityTrace()
                : new ExplainabilityTrace()
        };

        if (root.TryGetProperty("payload", out var payloadEl) && payloadEl.ValueKind != JsonValueKind.Null)
        {
            var typeName = finding.PayloadType;
            var payloadType = FindingPayloadRegistry.ResolvePayloadType(typeName);
            if (payloadType is not null)
                finding.Payload = JsonSerializer.Deserialize(payloadEl.GetRawText(), payloadType, options);
            else
                finding.Payload = payloadEl.Clone();
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

    private static List<string> ReadStringList(JsonElement root, string name)
    {
        if (!root.TryGetProperty(name, out var el) || el.ValueKind != JsonValueKind.Array)
            return [];
        return el.EnumerateArray().Select(e => e.GetString() ?? "").Where(s => s.Length > 0).ToList();
    }

    private static Dictionary<string, string> ReadStringDict(JsonElement root, string name)
    {
        if (!root.TryGetProperty(name, out var el) || el.ValueKind != JsonValueKind.Object)
            return new Dictionary<string, string>();
        var d = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var p in el.EnumerateObject())
            d[p.Name] = p.Value.GetString() ?? "";
        return d;
    }
}
