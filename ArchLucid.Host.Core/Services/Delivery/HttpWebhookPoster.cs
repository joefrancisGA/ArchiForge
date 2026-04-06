using System.Diagnostics.CodeAnalysis;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

using ArchiForge.Decisioning.Advisory.Delivery;

namespace ArchiForge.Host.Core.Services.Delivery;

/// <summary>POSTs JSON to external webhook URLs (Teams, Slack, on-call receivers).</summary>
[ExcludeFromCodeCoverage(Justification = "Requires live HTTP endpoint; tested via integration tests and delivery-channel unit tests that mock IWebhookPoster.")]
public sealed class HttpWebhookPoster(IHttpClientFactory httpClientFactory) : IWebhookPoster
{
    private const string ClientName = "ArchiForgeWebhooks";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public async Task PostJsonAsync(string url, object payload, CancellationToken ct, WebhookPostOptions? options = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(url);
        ArgumentNullException.ThrowIfNull(payload);

        string json = JsonSerializer.Serialize(payload, payload.GetType(), JsonOptions);
        byte[] body = Encoding.UTF8.GetBytes(json);

        using HttpRequestMessage request = new(HttpMethod.Post, url);
        request.Content = new ByteArrayContent(body);
        request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json") { CharSet = "utf-8" };

        string? secret = options?.HmacSha256SharedSecret?.Trim();

        if (!string.IsNullOrEmpty(secret))
        {
            string hex = WebhookSignature.ComputeSha256Hex(secret, body);
            request.Headers.TryAddWithoutValidation(WebhookSignature.HeaderName, WebhookSignature.Prefix + hex);
        }

        HttpClient client = httpClientFactory.CreateClient(ClientName);
        using HttpResponseMessage response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
        response.EnsureSuccessStatusCode();
    }
}
