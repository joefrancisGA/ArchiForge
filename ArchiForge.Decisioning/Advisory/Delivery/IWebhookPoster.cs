namespace ArchiForge.Decisioning.Advisory.Delivery;

public interface IWebhookPoster
{
    /// <param name="options">Optional signing; global defaults may be applied by the host poster implementation.</param>
    Task PostJsonAsync(string url, object payload, CancellationToken ct, WebhookPostOptions? options = null);
}
