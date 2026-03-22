namespace ArchiForge.Decisioning.Advisory.Delivery;

public interface IWebhookPoster
{
    Task PostJsonAsync(string url, object payload, CancellationToken ct);
}
