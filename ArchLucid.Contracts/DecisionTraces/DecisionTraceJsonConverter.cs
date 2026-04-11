using System.Text.Json;
using System.Text.Json.Serialization;

namespace ArchLucid.Contracts.DecisionTraces;

/// <summary>
/// Preserves the historical wire shape <c>{ "kind", "runEvent"?, "ruleAudit"? }</c> while using
/// <see cref="RunEventTrace"/> / <see cref="RuleAuditTrace"/> as the CLR types (OpenAPI stays a single object schema).
/// </summary>
public sealed class DecisionTraceJsonConverter : JsonConverter<DecisionTrace>
{
    /// <inheritdoc />
    public override DecisionTrace Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using JsonDocument document = JsonDocument.ParseValue(ref reader);
        JsonElement root = document.RootElement;

        if (!root.TryGetProperty("kind", out JsonElement kindElement))

            throw new JsonException("Decision trace JSON must include a numeric \"kind\" discriminator.");

        int kindValue = kindElement.GetInt32();

        if (!Enum.IsDefined(typeof(DecisionTraceKind), kindValue))

            throw new JsonException($"Unknown decision trace kind value '{kindValue}'.");

        DecisionTraceKind kind = (DecisionTraceKind)kindValue;

        if (kind == DecisionTraceKind.RunEvent)
        {
            if (!root.TryGetProperty("runEvent", out JsonElement runEventElement) || runEventElement.ValueKind == JsonValueKind.Null)

                throw new JsonException("RunEvent trace requires a non-null \"runEvent\" object.");

            RunEventTracePayload? payload = runEventElement.Deserialize<RunEventTracePayload>(options);

            return payload is null ? throw new JsonException("RunEvent trace \"runEvent\" deserialized to null.") : RunEventTrace.From(payload);
        }

        if (kind == DecisionTraceKind.RuleAudit)
        {
            if (!root.TryGetProperty("ruleAudit", out JsonElement ruleAuditElement) || ruleAuditElement.ValueKind == JsonValueKind.Null)

                throw new JsonException("RuleAudit trace requires a non-null \"ruleAudit\" object.");

            RuleAuditTracePayload? payload = ruleAuditElement.Deserialize<RuleAuditTracePayload>(options);

            return payload is null ? throw new JsonException("RuleAudit trace \"ruleAudit\" deserialized to null.") : RuleAuditTrace.From(payload);
        }

        throw new JsonException($"Unsupported decision trace kind '{kind}'.");
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, DecisionTrace value, JsonSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(value);

        writer.WriteStartObject();
        writer.WriteNumber("kind", (int)value.Kind);

        switch (value)
        {
            case RunEventTrace runEvent:
                writer.WritePropertyName("runEvent");
                JsonSerializer.Serialize(writer, runEvent.RunEvent, options);
                break;

            case RuleAuditTrace ruleAudit:
                writer.WritePropertyName("ruleAudit");
                JsonSerializer.Serialize(writer, ruleAudit.RuleAudit, options);
                break;

            default:
                throw new NotSupportedException($"Unsupported decision trace runtime type '{value.GetType().Name}'.");
        }

        writer.WriteEndObject();
    }
}
