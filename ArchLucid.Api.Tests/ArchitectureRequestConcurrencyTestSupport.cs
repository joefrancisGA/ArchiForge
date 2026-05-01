using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ArchLucid.Api.Tests;

/// <summary>
///     Shared helpers for parallel <c>POST /v1/architecture/request</c> bursts (idempotency + transient 503 retry).
/// </summary>
internal static class ArchitectureRequestConcurrencyTestSupport
{
    internal static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true, Converters = { new JsonStringEnumConverter(null) }
    };

    internal static StringContent JsonContent(object value)
    {
        string json = JsonSerializer.Serialize(value, JsonOptions);
        return new StringContent(json, Encoding.UTF8, "application/json");
    }

    internal static async Task<HttpResponseMessage[]> PostParallelArchitectureRequestAsync(
        HttpClient client,
        object body,
        string idempotencyKey,
        int parallel,
        CancellationToken cancellationToken = default)
    {
        // Per-operation timeout: cannot assign HttpClient.Timeout after the first request (runtime throws). Cold CI
        // SQL + DbUp + sp_getapplock can exceed 100s per slot, so use a long linked token for parallel bursts.
        TimeSpan operationTimeout = parallel > 1 ? TimeSpan.FromMinutes(6) : TimeSpan.FromSeconds(100);

        using CancellationTokenSource timeoutCts = new();
        timeoutCts.CancelAfter(operationTimeout);

        using CancellationTokenSource linked = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken, timeoutCts.Token);
        CancellationToken ct = linked.Token;

        Task<HttpResponseMessage>[] tasks = new Task<HttpResponseMessage>[parallel];

        for (int i = 0; i < parallel; i++)
        {
            HttpRequestMessage request = new(HttpMethod.Post, "/v1/architecture/request")
            {
                Content = JsonContent(body)
            };

            request.Headers.TryAddWithoutValidation("Idempotency-Key", idempotencyKey);
            tasks[i] = client.SendAsync(request, ct);
        }

        return await Task.WhenAll(tasks);
    }

    internal static async Task<HttpResponseMessage> PostSingleArchitectureRequestAsync(
        HttpClient client,
        object body,
        string idempotencyKey,
        CancellationToken cancellationToken)
    {
        HttpRequestMessage request = new(HttpMethod.Post, "/v1/architecture/request") { Content = JsonContent(body) };

        request.Headers.TryAddWithoutValidation("Idempotency-Key", idempotencyKey);

        return await client.SendAsync(request, cancellationToken);
    }

    /// <summary>
    ///     Replays any <see cref="HttpStatusCode.ServiceUnavailable" /> slots with single POSTs (same idempotency key) until
    ///     success or max attempts.
    /// </summary>
    internal static async Task<HttpResponseMessage[]> ResolveServiceUnavailablePerResponseAsync(
        HttpClient client,
        object body,
        string idempotencyKey,
        HttpResponseMessage[] responses,
        int maxPerSlotAttempts,
        CancellationToken cancellationToken)
    {
        for (int i = 0; i < responses.Length; i++)
        {
            int delayMs = 250;

            for (int attempt = 0;
                 attempt < maxPerSlotAttempts && responses[i].StatusCode == HttpStatusCode.ServiceUnavailable;
                 attempt++)
            {
                responses[i].Dispose();
                await Task.Delay(delayMs, cancellationToken);
                delayMs = Math.Min(delayMs * 2, 4000);
                responses[i] =
                    await PostSingleArchitectureRequestAsync(client, body, idempotencyKey, cancellationToken);
            }
        }

        return responses;
    }

    /// <summary>
    ///     Under parallel POST, SQL can briefly return errors mapped to HTTP 503; retry the whole burst with backoff.
    /// </summary>
    internal static async Task<HttpResponseMessage[]> PostParallelArchitectureRequestWithTransientRetryAsync(
        HttpClient client,
        object body,
        string idempotencyKey,
        int parallel,
        int maxAttempts,
        int initialDelayMilliseconds,
        CancellationToken cancellationToken)
    {
        int delayMilliseconds = initialDelayMilliseconds;
        HttpResponseMessage[] responses =
            await PostParallelArchitectureRequestAsync(client, body, idempotencyKey, parallel, cancellationToken);

        for (int attempt = 0;
             attempt < maxAttempts - 1 && responses.Any(static r => r.StatusCode == HttpStatusCode.ServiceUnavailable);
             attempt++)
        {
            DisposeAll(responses);
            await Task.Delay(delayMilliseconds, cancellationToken);
            delayMilliseconds = Math.Min(delayMilliseconds * 2, 4000);
            responses = await PostParallelArchitectureRequestAsync(client, body, idempotencyKey, parallel,
                cancellationToken);
        }

        return responses;
    }

    internal static void DisposeAll(HttpResponseMessage[] responses)
    {
        foreach (HttpResponseMessage response in responses)
        {
            response.Dispose();
        }
    }
}
