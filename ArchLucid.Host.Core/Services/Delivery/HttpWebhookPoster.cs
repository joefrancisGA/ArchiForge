using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

using ArchLucid.Decisioning.Advisory.Delivery;

using Microsoft.Extensions.Logging;

namespace ArchLucid.Host.Core.Services.Delivery;

/// <summary>POSTs JSON to external webhook URLs (Teams, Slack, on-call receivers).</summary>
[ExcludeFromCodeCoverage(Justification = "Requires live HTTP endpoint; tested via integration tests and delivery-channel unit tests that mock IWebhookPoster.")]
public sealed class HttpWebhookPoster(ILogger<HttpWebhookPoster> logger, IHttpClientFactory httpClientFactory) : IWebhookPoster
{
    private const string ClientName = "ArchLucidWebhooks";
    private const int MaxAttempts = 4;
    private const int InitialBackoffMilliseconds = 200;
    private const int MaxBackoffMilliseconds = 10000;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public async Task PostJsonAsync(string url, object payload, CancellationToken ct, WebhookPostOptions? options = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(url);
        ArgumentNullException.ThrowIfNull(payload);

        Uri? webhookUri = Uri.TryCreate(url, UriKind.Absolute, out Uri? absolute) ? absolute : null;
        HttpClient client = httpClientFactory.CreateClient(ClientName);

        string json = JsonSerializer.Serialize(payload, payload.GetType(), JsonOptions);
        byte[] body = Encoding.UTF8.GetBytes(json);

        for (int attempt = 0; attempt < MaxAttempts; attempt++)
        {
            ct.ThrowIfCancellationRequested();

            using HttpRequestMessage request = new(HttpMethod.Post, url);
            request.Content = new ByteArrayContent(body);
            request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json") { CharSet = "utf-8" };

            string? secret = options?.HmacSha256SharedSecret?.Trim();

            if (!string.IsNullOrEmpty(secret))
            {
                string hex = WebhookSignature.ComputeSha256Hex(secret, body);
                request.Headers.TryAddWithoutValidation(WebhookSignature.HeaderName, WebhookSignature.Prefix + hex);
            }

            try
            {
                using HttpResponseMessage response = await client
                    .SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct)
                    .ConfigureAwait(false);

                if (response.IsSuccessStatusCode)
                {
                    if (attempt > 0 && logger.IsEnabled(LogLevel.Information))
                    {
                        logger.LogInformation(
                            "Webhook POST to {WebhookHost} succeeded after {Retries} retries.",
                            webhookUri?.Host ?? "(unknown-host)",
                            attempt);
                    }

                    return;
                }

                HttpStatusCode code = response.StatusCode;

                if (IsTransientHttpStatus(code) && attempt < MaxAttempts - 1)
                {
                    LogTransientWebhookFailure(code, webhookUri?.Host, attempt);
                    await BackoffDelayAsync(attempt, ct).ConfigureAwait(false);
                    continue;
                }

                response.EnsureSuccessStatusCode();
            }
            catch (HttpRequestException ex) when (attempt < MaxAttempts - 1)
            {
                logger.LogWarning(
                    ex,
                    "Webhook POST to {WebhookHost} failed during transport (attempt {Attempt}/{MaxAttempts}). Retrying.",
                    webhookUri?.Host ?? "(unknown-host)",
                    attempt + 1,
                    MaxAttempts);
                await BackoffDelayAsync(attempt, ct).ConfigureAwait(false);
            }
        }
    }

    private void LogTransientWebhookFailure(HttpStatusCode code, string? webhookHost, int zeroBasedAttemptAfterFailure)
    {
        if (!logger.IsEnabled(LogLevel.Warning))
            return;

        logger.LogWarning(
            "Webhook POST returned {StatusCode} for {WebhookHost}; attempt {Attempt} of {MaxAttempts}. Retrying.",
            (int)code,
            webhookHost ?? "(unknown-host)",
            zeroBasedAttemptAfterFailure + 1,
            MaxAttempts);
    }

    private static Task BackoffDelayAsync(int zeroBasedAttempt, CancellationToken ct)
    {
        int milliseconds = InitialBackoffMilliseconds * (1 << zeroBasedAttempt);
        milliseconds = Math.Min(MaxBackoffMilliseconds, milliseconds);
        return Task.Delay(TimeSpan.FromMilliseconds(milliseconds), ct);
    }

    /// <remarks>Retries are conservative: timeouts, explicit rate-limiting, or server faults.</remarks>
    private static bool IsTransientHttpStatus(HttpStatusCode code)
    {
        if (code == HttpStatusCode.RequestTimeout || code == HttpStatusCode.TooManyRequests)
            return true;

        return (int)code >= 500;
    }
}
