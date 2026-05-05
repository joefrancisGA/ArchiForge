using ArchLucid.Host.Core.Configuration;
using ArchLucid.Notifications;

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

        return inner.PostJsonAsync(url, payload, ct, !string.IsNullOrEmpty(secret) ? new WebhookPostOptions { HmacSha256SharedSecret = secret, EventType = options?.EventType, TenantId = options?.TenantId, } : options);
    }
}
