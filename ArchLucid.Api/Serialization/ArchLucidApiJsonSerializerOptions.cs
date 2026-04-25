using System.Text.Json;
using System.Text.Json.Serialization;

namespace ArchLucid.Api.Serialization;

/// <summary>
///     JSON options aligned with <c>AddArchLucidMvc().AddJsonOptions</c> for byte-stable serialization (ETags,
///     tests).
/// </summary>
internal static class ArchLucidApiJsonSerializerOptions
{
    public static readonly JsonSerializerOptions Web = Create();

    private static JsonSerializerOptions Create()
    {
        JsonSerializerOptions options = new(JsonSerializerDefaults.Web)
        {
            DictionaryKeyPolicy = JsonNamingPolicy.CamelCase
        };
        options.Converters.Add(new JsonStringEnumConverter(null));

        return options;
    }
}
