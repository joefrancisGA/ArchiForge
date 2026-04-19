using System.Text.Json;

namespace ArchLucid.Core.Integration;

/// <summary>Derives Service Bus user application properties for subscription SQL filters (message body is not filterable in Service Bus rules).</summary>
public static class IntegrationEventServiceBusApplicationProperties
{
    /// <summary>User property name for governance promotion environment (lowercase), used with Logic App / SQL subscription filters.</summary>
    public const string PromotionEnvironmentPropertyName = "promotion_environment";

    /// <summary>Resolves optional application properties from the UTF-8 JSON payload for outbox drain or direct publish.</summary>
    public static IReadOnlyDictionary<string, object>? TryResolveForPublish(string eventType, ReadOnlyMemory<byte> payloadUtf8)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(eventType);

        if (!IntegrationEventTypes.AreEquivalent(eventType, IntegrationEventTypes.GovernancePromotionActivatedV1))
        {
            return null;
        }

        if (payloadUtf8.IsEmpty)
        {
            return null;
        }

        try
        {
            using JsonDocument doc = JsonDocument.Parse(payloadUtf8.Span);

            if (!doc.RootElement.TryGetProperty("environment", out JsonElement envEl))
            {
                return null;
            }

            string? env = envEl.GetString();

            if (string.IsNullOrWhiteSpace(env))
            {
                return null;
            }

            Dictionary<string, object> map = new(StringComparer.Ordinal)
            {
                [PromotionEnvironmentPropertyName] = env.Trim().ToLowerInvariant(),
            };

            return map;
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
