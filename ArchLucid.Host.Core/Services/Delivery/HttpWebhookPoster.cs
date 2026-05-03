using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

using ArchLucid.Core.Diagnostics;
using ArchLucid.Decisioning.Advisory.Delivery;

namespace ArchLucid.Host.Core.Services.Delivery;

/// <summary>POSTs JSON to external webhook URLs (Teams, Slack, on-call receivers).</summary>
public sealed class HttpWebhookPoster(ILogger<HttpWebhookPoster> logger, IHttpClientFactory httpClientFactory) : IWebhookPoster
{
    public const string WebhookHttpClientName = "ArchLucidWebhooks";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    /// <inheritdoc />
    public async Task PostJsonAsync(string url, object payload, CancellationToken ct, WebhookPostOptions? options = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(url);
        ArgumentNullException.ThrowIfNull(payload);

        _ = Uri.TryCreate(url, UriKind.Absolute, out Uri? webhookUri);

        HttpClient client = httpClientFactory.CreateClient(WebhookHttpClientName);

        string json = JsonSerializer.Serialize(payload, payload.GetType(), JsonOptions);
        byte[] body = Encoding.UTF8.GetBytes(json);

        string telemetryEventType = TelemetryEventLabel(options?.EventType);
        Guid telemetryTenantId = options?.TenantId ?? Guid.Empty;
        string targetAuthority = TelemetryTargetAuthority(webhookUri);

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

        long startTicks = Stopwatch.GetTimestamp();

        HttpResponseMessage transportResponse;

        try
        {
            transportResponse =
                await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            RecordWebhookOutboundDeliveryAttempt(
                statusCode: null,
                startTicks,
                targetAuthority,
                telemetryEventType,
                telemetryTenantId,
                succeeded: false);

            throw;
        }
        catch (HttpRequestException ex)
        {
            RecordWebhookOutboundDeliveryAttempt(
                statusCode: null,
                startTicks,
                targetAuthority,
                telemetryEventType,
                telemetryTenantId,
                succeeded: false);

            if (logger.IsEnabled(LogLevel.Warning))
                logger.LogWarning(ex, "Webhook outbound POST transport error after outbound retries.");

            throw;
        }
        catch (Exception ex)
        {
            RecordWebhookOutboundDeliveryAttempt(
                statusCode: null,
                startTicks,
                targetAuthority,
                telemetryEventType,
                telemetryTenantId,
                succeeded: false);

            if (logger.IsEnabled(LogLevel.Warning))
                logger.LogWarning(ex, "Webhook outbound POST threw before a response.");

            throw;
        }

        using HttpResponseMessage response = transportResponse;

        HttpStatusCode code = response.StatusCode;

        bool succeeded = response.IsSuccessStatusCode;

        RecordWebhookOutboundDeliveryAttempt(
            (int?)code,
            startTicks,
            targetAuthority,
            telemetryEventType,
            telemetryTenantId,
            succeeded);

        if (!succeeded)
            throw new HttpRequestException($"Outbound webhook failed with HTTP {(int)code}.", inner: null, statusCode: code);
    }

    private static string TelemetryTargetAuthority(Uri? absoluteUri)
    {
        if (absoluteUri is null || !absoluteUri.IsAbsoluteUri)
            return string.Empty;

        return absoluteUri.GetLeftPart(UriPartial.Authority);
    }

    private static string TelemetryEventLabel(string? eventTypeFromOptions)
    {
        string trimmed = eventTypeFromOptions?.Trim() ?? string.Empty;

        return string.IsNullOrEmpty(trimmed) ? "unknown" : trimmed;
    }

    private static long DurationMillisecondsRounded(long attemptStartTicks)
    {
        double ms = Stopwatch.GetElapsedTime(attemptStartTicks).TotalMilliseconds;

        switch (Math.Round(ms, MidpointRounding.AwayFromZero))
        {
            case <= 0:
                return 0;

            case > long.MaxValue:
                return long.MaxValue;

            default:
                return (long)Math.Round(ms, MidpointRounding.AwayFromZero);
        }
    }

    private void RecordWebhookOutboundDeliveryAttempt(
        int? statusCode,
        long attemptStartTicks,
        string targetAuthority,
        string telemetryEventType,
        Guid telemetryTenantId,
        bool succeeded)
    {
        long durationMilliseconds = DurationMillisecondsRounded(attemptStartTicks);

        TagList deliveriesTags =
        [
            KeyValuePair.Create<string, object?>("event_type", telemetryEventType),
            KeyValuePair.Create<string, object?>(
                "succeeded",
                succeeded ? "true" : "false"),
        ];

        ArchLucidInstrumentation.WebhookDeliveries.Add(1, deliveriesTags);

        TagList durationTags = new() { { "event_type", telemetryEventType } };

        ArchLucidInstrumentation.WebhookDeliveryDurationMilliseconds.Record(durationMilliseconds, durationTags);

        Dictionary<string, object?> scopeState = WebhookOutboundScopeState(
            statusCode,
            durationMilliseconds,
            targetAuthority,
            telemetryEventType,
            telemetryTenantId,
            succeeded);

        using IDisposable? loggingScope = logger.BeginScope(scopeState);

        LogLevel lvl = succeeded ? LogLevel.Information : LogLevel.Warning;

        logger.Log(lvl, "Webhook outbound HTTP POST attempt completed.");
    }

    private static Dictionary<string, object?> WebhookOutboundScopeState(
        int? statusCode,
        long durationMilliseconds,
        string targetAuthority,
        string telemetryEventType,
        Guid telemetryTenantId,
        bool succeeded) =>
        new()
        {
            ["archlucid.webhook.status_code"] = statusCode,
            ["archlucid.webhook.duration_ms"] = durationMilliseconds,
            ["archlucid.webhook.target_authority"] = targetAuthority,
            ["archlucid.webhook.event_type"] = telemetryEventType,
            ["archlucid.webhook.tenant_id"] = telemetryTenantId,
            ["archlucid.webhook.succeeded"] = succeeded,
        };
}
