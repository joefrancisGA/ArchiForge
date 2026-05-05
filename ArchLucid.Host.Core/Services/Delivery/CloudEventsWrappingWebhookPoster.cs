using System.Text.Json;
using System.Text.Json.Serialization;

using ArchLucid.Notifications;
using ArchLucid.Host.Core.Configuration;

using Microsoft.Extensions.Options;

namespace ArchLucid.Host.Core.Services.Delivery;

/// <summary>
/// Wraps JSON webhook payloads in a <a href="https://github.com/cloudevents/spec/blob/v1.0.2/cloudevents/formats/json-format.md">CloudEvents 1.0 JSON</a> envelope
/// before delegating to the inner poster (HMAC still applies to the envelope when configured).
/// </summary>
public sealed class CloudEventsWrappingWebhookPoster(
    IOptionsMonitor<WebhookDeliveryOptions> deliveryOptions,
    IWebhookPoster inner) : IWebhookPoster
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    /// <inheritdoc />
    public Task PostJsonAsync(string url, object payload, CancellationToken ct, WebhookPostOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(payload);

        if (!deliveryOptions.CurrentValue.UseCloudEventsEnvelope)
            return inner.PostJsonAsync(url, payload, ct, options);

        string source = string.IsNullOrWhiteSpace(deliveryOptions.CurrentValue.CloudEventsSource)
            ? "/archlucid/webhooks"
            : deliveryOptions.CurrentValue.CloudEventsSource.Trim();

        string type = string.IsNullOrWhiteSpace(deliveryOptions.CurrentValue.CloudEventsType)
            ? "com.archlucid.webhook.payload"
            : deliveryOptions.CurrentValue.CloudEventsType.Trim();

        CloudEventV10 envelope = CloudEventV10.Create(type, source, payload);

        return inner.PostJsonAsync(url, envelope, ct, options);
    }

    /// <summary>Minimal CloudEvents 1.0 JSON shape for webhook receivers.</summary>
    internal sealed class CloudEventV10
    {
        [JsonPropertyName("specversion")]
        public string SpecVersion
        {
            get;
            init;
        } = "1.0";

        [JsonPropertyName("type")]
        public required string Type
        {
            get;
            init;
        }

        [JsonPropertyName("source")]
        public required string Source
        {
            get;
            init;
        }

        [JsonPropertyName("id")]
        public required string Id
        {
            get;
            init;
        }

        [JsonPropertyName("time")]
        public string Time
        {
            get;
            init;
        } = "";

        [JsonPropertyName("datacontenttype")]
        public string DataContentType
        {
            get;
            init;
        } = "application/json";

        [JsonPropertyName("data")]
        public required JsonElement Data
        {
            get;
            init;
        }

        public static CloudEventV10 Create(string type, string source, object data)
        {
            byte[] raw = JsonSerializer.SerializeToUtf8Bytes(data, data.GetType(), JsonOptions);
            JsonElement element = JsonSerializer.Deserialize<JsonElement>(raw);

            return new CloudEventV10
            {
                Type = type,
                Source = source,
                Id = Guid.NewGuid().ToString("D"),
                Time = DateTime.UtcNow.ToString("O"),
                Data = element,
            };
        }
    }
}
