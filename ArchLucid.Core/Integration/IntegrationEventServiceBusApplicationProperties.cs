using System.Text.Json;

namespace ArchLucid.Core.Integration;

/// <summary>
///     Derives Service Bus user application properties for subscription SQL filters (message body is not filterable
///     in Service Bus rules).
/// </summary>
public static class IntegrationEventServiceBusApplicationProperties
{
    /// <summary>
    ///     User property name for governance promotion environment (lowercase), used with Logic App / SQL subscription
    ///     filters.
    /// </summary>
    public const string PromotionEnvironmentPropertyName = "promotion_environment";

    /// <summary>
    ///     User property for <see cref="IntegrationEventTypes.AlertFiredV1" /> / resolved correlation (normalized
    ///     lowercase for SQL filters).
    /// </summary>
    public const string SeverityPropertyName = "severity";

    /// <summary>
    ///     User property carrying <c>deduplicationKey</c> from alert payloads (snake_case for SQL / Logic App parity with
    ///     <see cref="PromotionEnvironmentPropertyName" />).
    /// </summary>
    public const string DeduplicationKeyPropertyName = "deduplication_key";

    /// <summary>Resolves optional application properties from the UTF-8 JSON payload for outbox drain or direct publish.</summary>
    public static IReadOnlyDictionary<string, object>? TryResolveForPublish(string eventType,
        ReadOnlyMemory<byte> payloadUtf8)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(eventType);

        if (payloadUtf8.IsEmpty)
            return null;

        if (IntegrationEventTypes.AreEquivalent(eventType, IntegrationEventTypes.GovernancePromotionActivatedV1))
            return TryResolveGovernancePromotionActivated(payloadUtf8);

        if (IntegrationEventTypes.AreEquivalent(eventType, IntegrationEventTypes.AlertFiredV1))
            return TryResolveAlertFired(payloadUtf8);

        return IntegrationEventTypes.AreEquivalent(eventType, IntegrationEventTypes.AlertResolvedV1)
            ? TryResolveAlertResolved(payloadUtf8)
            : null;
    }

    private static IReadOnlyDictionary<string, object>? TryResolveGovernancePromotionActivated(
        ReadOnlyMemory<byte> payloadUtf8)
    {
        try
        {
            using JsonDocument doc = JsonDocument.Parse(payloadUtf8);

            if (!doc.RootElement.TryGetProperty("environment", out JsonElement envEl))
                return null;

            string? env = envEl.GetString();

            if (string.IsNullOrWhiteSpace(env))
                return null;

            Dictionary<string, object> map = new(StringComparer.Ordinal)
            {
                [PromotionEnvironmentPropertyName] = env.Trim().ToLowerInvariant()
            };

            return map;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static IReadOnlyDictionary<string, object>? TryResolveAlertFired(ReadOnlyMemory<byte> payloadUtf8)
    {
        try
        {
            using JsonDocument doc = JsonDocument.Parse(payloadUtf8);
            Dictionary<string, object> map = new(StringComparer.Ordinal);

            if (doc.RootElement.TryGetProperty("severity", out JsonElement sevEl))
            {
                string? sev = sevEl.GetString();

                if (!string.IsNullOrWhiteSpace(sev))
                    map[SeverityPropertyName] = sev.Trim().ToLowerInvariant();
            }

            if (!doc.RootElement.TryGetProperty("deduplicationKey", out JsonElement dedupeEl))
                return map.Count > 0 ? map : null;

            string? dedupe = dedupeEl.GetString();

            if (!string.IsNullOrWhiteSpace(dedupe))
                map[DeduplicationKeyPropertyName] = dedupe.Trim();

            return map.Count > 0 ? map : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static IReadOnlyDictionary<string, object>? TryResolveAlertResolved(ReadOnlyMemory<byte> payloadUtf8)
    {
        try
        {
            using JsonDocument doc = JsonDocument.Parse(payloadUtf8);

            if (!doc.RootElement.TryGetProperty("deduplicationKey", out JsonElement dedupeEl))
                return null;

            string? dedupe = dedupeEl.GetString();

            if (string.IsNullOrWhiteSpace(dedupe))
                return null;

            Dictionary<string, object> map = new(StringComparer.Ordinal)
            {
                [DeduplicationKeyPropertyName] = dedupe.Trim()
            };

            return map;
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
