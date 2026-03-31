namespace ArchiForge.Api.Configuration;

/// <summary>Configuration for outbound digest/alert webhook HTTP posts.</summary>
public sealed class WebhookDeliveryOptions
{
    public const string SectionName = "WebhookDelivery";

    /// <summary>When true, uses <see cref="HttpWebhookPoster"/> instead of <see cref="Services.Delivery.FakeWebhookPoster"/>.</summary>
    public bool UseHttpClient { get; set; }

    /// <summary>Optional shared secret for <c>X-ArchiForge-Webhook-Signature</c> on all webhook bodies (UTF-8 JSON).</summary>
    public string? HmacSha256SharedSecret { get; set; }
}
