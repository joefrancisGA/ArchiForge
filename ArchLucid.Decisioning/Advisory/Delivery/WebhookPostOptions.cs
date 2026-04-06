using System.Diagnostics.CodeAnalysis;

namespace ArchiForge.Decisioning.Advisory.Delivery;

/// <summary>Optional parameters for <see cref="IWebhookPoster.PostJsonAsync"/>.</summary>
[ExcludeFromCodeCoverage(Justification = "Options DTO for webhook calls; no logic.")]
public sealed class WebhookPostOptions
{
    /// <summary>
    /// When non-empty, the poster adds <c>X-ArchiForge-Webhook-Signature: sha256=&lt;hex&gt;</c> over the raw UTF-8 JSON body.
    /// </summary>
    public string? HmacSha256SharedSecret { get; init; }
}
