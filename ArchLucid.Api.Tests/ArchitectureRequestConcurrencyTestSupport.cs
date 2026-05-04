using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ArchLucid.Api.Tests;

/// <summary>
///     Shared helpers for parallel <c>POST /v1/architecture/request</c> bursts (idempotency + transient 503 retry).
/// </summary>
internal static class ArchitectureRequestConcurrencyTestSupport
{
    /// <summary>
    ///     Default <see cref="HttpClient.Timeout" /> is 100s; create-run idempotency uses <c>sp_getapplock</c> with a
    ///     wait budget up to <c>CreateRun:DistributedIdempotencyLockTimeoutMilliseconds</c> (300s in greenfield SQL tests).
    ///     Parallel POSTs serialize on that lock; cold CI SQL + greenfield create-run can keep contenders blocked well
    ///     beyond the default <see cref="HttpClient.Timeout" /> (100s) unless the client and per-burst token are raised.
    /// </summary>
    private static readonly TimeSpan ArchitectureRequestBurstHttpTimeout = TimeSpan.FromMinutes(25);

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
        if (parallel > 1)
            AlignHttpClientTimeoutForSqlIdempotencyLockChain(client);

        // Per-operation timeout: cannot assign HttpClient.Timeout after the first request (runtime throws). Cold CI
        // SQL + DbUp + serialized sp_getapplock chains can exceed many minutes (N slots x create-run duration).
        TimeSpan operationTimeout = parallel > 1 ? ArchitectureRequestBurstHttpTimeout : TimeSpan.FromSeconds(100);

        using CancellationTokenSource timeoutCts = new();
        timeoutCts.CancelAfter(operationTimeout);

        using CancellationTokenSource linked = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken, timeoutCts.Token);
        CancellationToken ct = linked.Token;

        Task<HttpResponseMessage>[] tasks = new Task<HttpResponseMessage>[parallel];

        for (int i = 0; i < parallel; i++)
        {
            tasks[i] = PostArchitectureRequestAndBufferAsync(client, body, idempotencyKey, ct);
        }

        return await Task.WhenAll(tasks);
    }

    internal static Task<HttpResponseMessage> PostSingleArchitectureRequestAsync(
        HttpClient client,
        object body,
        string idempotencyKey,
        CancellationToken cancellationToken)
    {
        return PostArchitectureRequestAndBufferAsync(client, body, idempotencyKey, cancellationToken);
    }

    /// <summary>
    ///     Uses <see cref="HttpCompletionOption.ResponseHeadersRead" /> then buffers the body under
    ///     <paramref name="cancellationToken" />, which is more reliable for long-running create-run + idempotency waits
    ///     against <see cref="Microsoft.AspNetCore.TestHost.TestServer" /> than default response buffering alone.
    /// </summary>
    private static async Task<HttpResponseMessage> PostArchitectureRequestAndBufferAsync(
        HttpClient client,
        object body,
        string idempotencyKey,
        CancellationToken cancellationToken)
    {
        HttpRequestMessage request = new(HttpMethod.Post, "/v1/architecture/request") { Content = JsonContent(body) };

        request.Headers.TryAddWithoutValidation("Idempotency-Key", idempotencyKey);

        HttpResponseMessage response =
            await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

        await response.Content.LoadIntoBufferAsync(cancellationToken);

        return response;
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
        AlignHttpClientTimeoutForSqlIdempotencyLockChain(client);

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

    private static void AlignHttpClientTimeoutForSqlIdempotencyLockChain(HttpClient client)
    {
        if (client.Timeout < ArchitectureRequestBurstHttpTimeout)
            client.Timeout = ArchitectureRequestBurstHttpTimeout;
    }
}
