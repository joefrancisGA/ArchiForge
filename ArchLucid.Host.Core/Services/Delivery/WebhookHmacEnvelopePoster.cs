using ArchiForge.Host.Core.Configuration;

using ArchiForge.Decisioning.Advisory.Delivery;

using Microsoft.Extensions.Options;

namespace ArchiForge.Host.Core.Services.Delivery;

/// <summary>
/// Merges <see cref="WebhookDeliveryOptions.HmacSha256SharedSecret"/> with per-call <see cref="WebhookPostOptions"/>.
/// </summary>
public sealed class WebhookHmacEnvelopePoster(IOptionsMonitor<WebhookDeliveryOptions> deliveryOptions, IWebhookPoster inner)
    : IWebhookPoster
{
    public Task PostJsonAsync(string url, object payload, CancellationToken ct, WebhookPostOptions? options = null)
    {
        string? fromCall = options?.HmacSha256SharedSecret?.Trim();
        string? fromConfig = deliveryOptions.CurrentValue.HmacSha256SharedSecret?.Trim();
        string? secret = !string.IsNullOrEmpty(fromCall) ? fromCall : fromConfig;

        WebhookPostOptions? merged = string.IsNullOrEmpty(secret)
            ? options
            : new WebhookPostOptions { HmacSha256SharedSecret = secret };

        return inner.PostJsonAsync(url, payload, ct, merged);
    }
}
