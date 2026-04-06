using System.Text.Json;
using System.Text.Json.Serialization;

namespace ArchiForge.Core.Integration;

/// <summary>Shared JSON options for UTF-8 integration event payloads.</summary>
public static class IntegrationEventJson
{
    public static JsonSerializerOptions Options { get; } = Create();

    private static JsonSerializerOptions Create()
    {
        JsonSerializerOptions options = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        };

        return options;
    }
}
