using System.Text.Json;
using ArchiForge.Decisioning.Advisory.Delivery;

namespace ArchiForge.Api.Services.Delivery;

public sealed class FakeWebhookPoster(ILogger<FakeWebhookPoster> logger) : IWebhookPoster
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = false };

    public Task PostJsonAsync(string url, object payload, CancellationToken ct)
    {
        _ = ct;
        logger.LogInformation(
            "[FakeWebhook] Url={Url} Payload={Payload}",
            url,
            JsonSerializer.Serialize(payload, JsonOptions));
        return Task.CompletedTask;
    }
}
