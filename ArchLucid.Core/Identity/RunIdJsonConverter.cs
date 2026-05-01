using System.Text.Json;
using System.Text.Json.Serialization;

namespace ArchLucid.Core.Identity;

/// <summary>JSON: run id as GUID string (round-trips with <see cref="RunId" />).</summary>
public sealed class RunIdJsonConverter : JsonConverter<RunId>
{
    /// <inheritdoc />
    public override RunId Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.String)
            throw new JsonException("Expected string for RunId.");

        string? s = reader.GetString();
        if (string.IsNullOrWhiteSpace(s) || !Guid.TryParse(s, out Guid g))
            throw new JsonException("RunId must be a valid GUID string.");

        return new RunId(g);
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, RunId value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.Value);
    }
}
