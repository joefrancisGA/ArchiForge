namespace ArchiForge.Host.Core.Configuration;

/// <summary>Configuration for outbound digest/alert webhook HTTP posts.</summary>
public sealed class WebhookDeliveryOptions
{
    public const string SectionName = "WebhookDelivery";

    /// <summary>When true, uses <see cref="HttpWebhookPoster"/> instead of <see cref="Services.Delivery.FakeWebhookPoster"/>.</summary>
    public bool UseHttpClient { get; set; }

    /// <summary>Optional shared secret for <c>X-ArchiForge-Webhook-Signature</c> on all webhook bodies (UTF-8 JSON).</summary>
    public string? HmacSha256SharedSecret { get; set; }

    /// <summary>When true, wraps payloads in a CloudEvents 1.0 JSON envelope before signing and POSTing.</summary>
    public bool UseCloudEventsEnvelope { get; set; }

    /// <summary>CloudEvents <c>source</c> URI reference (default <c>/archiforge/webhooks</c>).</summary>
    public string? CloudEventsSource { get; set; }

    /// <summary>CloudEvents <c>type</c> (default <c>com.archiforge.webhook.payload</c>).</summary>
    public string? CloudEventsType { get; set; }
}
