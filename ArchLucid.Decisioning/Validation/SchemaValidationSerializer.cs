using System.Text.Json;
using System.Text.Json.Serialization;

namespace ArchLucid.Decisioning.Validation;

public static class SchemaValidationSerializer
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = false,
        // JSON schemas expect string enum names (e.g. "Topology"); default numeric enums fail validation.
        Converters = { new JsonStringEnumConverter() }
    };

    public static string Serialize<T>(T value)
    {
        return JsonSerializer.Serialize(value, JsonOptions);
    }
}
