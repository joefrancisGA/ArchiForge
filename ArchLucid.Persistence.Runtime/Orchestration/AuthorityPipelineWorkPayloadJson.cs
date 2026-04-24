using System.Text.Json;

namespace ArchLucid.Persistence.Orchestration;

/// <summary>Shared JSON options for <see cref="AuthorityPipelineWorkPayload" /> serialization.</summary>
public static class AuthorityPipelineWorkPayloadJson
{
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

    public static string Serialize(AuthorityPipelineWorkPayload payload)
    {
        ArgumentNullException.ThrowIfNull(payload);

        return JsonSerializer.Serialize(payload, Options);
    }

    public static AuthorityPipelineWorkPayload? Deserialize(string json)
    {
        return string.IsNullOrWhiteSpace(json)
            ? null
            : JsonSerializer.Deserialize<AuthorityPipelineWorkPayload>(json, Options);
    }
}
