namespace ArchLucid.Notifications;

/// <summary>Outbound JSON POST to webhook URLs (Slack, Teams, on-call receivers).</summary>
public interface IWebhookPoster
{
    /// <param name="options">Optional signing; global defaults may be applied by the host poster implementation.</param>
    Task PostJsonAsync(
        string url,
        object payload,
        CancellationToken ct,
        WebhookPostOptions? options = null);
}
