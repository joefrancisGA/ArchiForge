using System.Text.Json;

using ArchiForge.Decisioning.Advisory.Delivery;

namespace ArchiForge.Host.Core.Services.Delivery;

public sealed class FakeWebhookPoster(ILogger<FakeWebhookPoster> logger) : IWebhookPoster
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = false };

    public Task PostJsonAsync(string url, object payload, CancellationToken ct, WebhookPostOptions? options = null)
    {
        _ = ct;
        string? sig = null;

        if (!string.IsNullOrWhiteSpace(options?.HmacSha256SharedSecret))
        {
            string json = JsonSerializer.Serialize(payload, payload.GetType(), JsonOptions);
            byte[] body = System.Text.Encoding.UTF8.GetBytes(json);
            sig = WebhookSignature.Prefix + WebhookSignature.ComputeSha256Hex(options.HmacSha256SharedSecret.Trim(), body);
        }

        logger.LogInformation(
            "[FakeWebhook] Url={Url} Signature={Signature} Payload={Payload}",
            url,
            sig ?? "(none)",
            JsonSerializer.Serialize(payload, JsonOptions));
        return Task.CompletedTask;
    }
}
