using ArchLucid.Host.Core.Configuration;
using ArchLucid.Decisioning.Advisory.Delivery;

using Microsoft.Extensions.Options;

namespace ArchLucid.Host.Core.Services.Delivery;

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

        if (!string.IsNullOrEmpty(secret))
        {
            return inner.PostJsonAsync(
                url,
                payload,
                ct,
                new WebhookPostOptions { HmacSha256SharedSecret = secret, EventType = options?.EventType, TenantId = options?.TenantId, });
        }

        return inner.PostJsonAsync(url, payload, ct, options);
    }
}
