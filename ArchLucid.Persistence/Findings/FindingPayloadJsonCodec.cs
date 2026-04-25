using System.Text.Json;

using ArchLucid.Decisioning.Findings;
using ArchLucid.Decisioning.Findings.Serialization;
using ArchLucid.Decisioning.Models;

namespace ArchLucid.Persistence.Findings;

/// <summary>
///     Serializes <see cref="ArchLucid.Decisioning.Models.Finding.Payload" /> to/from <c>PayloadJson</c> using the same
///     options as <see cref="FindingJsonConverter" /> so typed payloads round-trip.
/// </summary>
public static class FindingPayloadJsonCodec
{
    private static readonly JsonSerializerOptions Options = FindingsSerialization.CreateOptions();

    public static string? SerializePayload(object? payload)
    {
        if (payload is null)
            return null;

        if (payload is JsonElement element)
            return element.GetRawText();

        return JsonSerializer.Serialize(payload, payload.GetType(), Options);
    }

    public static object? DeserializePayload(string? json, string? payloadType)
    {
        if (string.IsNullOrWhiteSpace(json))
            return null;

        Type? resolved = FindingPayloadRegistry.ResolvePayloadType(payloadType);

        if (resolved is not null)
            return JsonSerializer.Deserialize(json, resolved, Options);

        using JsonDocument document = JsonDocument.Parse(json);
        return document.RootElement.Clone();
    }

    /// <summary>
    ///     After <see cref="ArchLucid.Persistence.Serialization.JsonEntitySerializer" /> deserializes a
    ///     <see cref="FindingsSnapshot" />, nested <see cref="Finding.Payload" /> values are often <see cref="JsonElement" />.
    ///     This aligns the JSON fallback read path with relational reads by materializing registered payload types.
    /// </summary>
    public static void HydrateJsonElementPayloads(IReadOnlyList<Finding> findings)
    {
        ArgumentNullException.ThrowIfNull(findings);

        foreach (Finding finding in findings)
        {
            if (finding.Payload is not JsonElement element)
                continue;

            finding.Payload = DeserializePayload(element.GetRawText(), finding.PayloadType);
        }
    }
}
