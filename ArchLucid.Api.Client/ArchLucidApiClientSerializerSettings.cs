using System.Text.Json;
using System.Text.Json.Serialization;

namespace ArchLucid.Api.Client.Generated;

/// <summary>
///     NSwag-generated <see cref="ArchLucidApiClient" /> defaults omit a global enum converter; the live API serializes
///     contract enums as JSON strings (ASP.NET <c>JsonStringEnumConverter</c>). Wire settings must match so typed
///     responses (create / execute / commit) deserialize without <see cref="JsonException" />.
/// </summary>
public partial class ArchLucidApiClient
{
    static partial void UpdateJsonSerializerSettings(JsonSerializerOptions settings)
    {
        settings.PropertyNameCaseInsensitive = true;
        settings.Converters.Add(new JsonStringEnumConverter());
    }
}
