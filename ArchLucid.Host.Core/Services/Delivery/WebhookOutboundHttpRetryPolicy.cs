using System.Net;
using System.Net.Http;

using Polly;
using Polly.Retry;

namespace ArchLucid.Host.Core.Services.Delivery;

/// <summary>
/// Polly outbound retry for the named <see cref="HttpWebhookPoster" /> client (<c>ArchLucidWebhooks</c>).
/// Registered on the webhook <see cref="IHttpClientBuilder" /> with <c>AddPolicyHandler</c>.
/// </summary>
/// <remarks>
/// Polly v8 uses <see cref="ResiliencePipeline{HttpResponseMessage}" />; bridging to legacy
/// <see cref="IAsyncPolicy{TResult}" /> preserves out-of-box <c>Microsoft.Extensions.Http.Polly</c> <c>AddPolicyHandler</c> integration.
/// The v7-era <c>AsyncRetryPolicy{T}</c> type does not compile under centrally managed Polly 8.x.
/// </remarks>
public static class WebhookOutboundHttpRetryPolicy
{
    public const int ProductionRetryAttempts = 3;

    /// <summary>
    /// Builds the production policy: handles <see cref="HttpRequestException" />, HTTP 408, any 5xx,
    /// and 429 (<see cref="HttpStatusCode.TooManyRequests" />). Exponential delays (~2 s, ~4 s, ~8 s) between attempts.
    /// </summary>
    public static IAsyncPolicy<HttpResponseMessage> Create() =>
        Create(static retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));

    /// <summary>
    /// Identical fault handling as the parameterless overload but callers may replace backoff (for example zero delay for tests).
    /// </summary>
    public static IAsyncPolicy<HttpResponseMessage> Create(Func<int, TimeSpan> sleepDurationProvider)
    {
        ArgumentNullException.ThrowIfNull(sleepDurationProvider);

        ResiliencePipeline<HttpResponseMessage> pipeline = new ResiliencePipelineBuilder<HttpResponseMessage>()
            .AddRetry(new RetryStrategyOptions<HttpResponseMessage>
            {
                MaxRetryAttempts = ProductionRetryAttempts,
                ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                    .Handle<HttpRequestException>()
                    .HandleResult(ShouldRetryOutboundWebhookResponse),
                DelayGenerator = args =>
                {
                    TimeSpan delay = sleepDurationProvider(args.AttemptNumber);

                    return new ValueTask<TimeSpan?>(delay);
                },
            })
            .Build();

        return pipeline.AsAsyncPolicy();
    }

    /// <summary>Matches <c>HandleTransientHttpError().OrResult(429)</c> from the historical Polly.Extensions.Http policy.</summary>
    private static bool ShouldRetryOutboundWebhookResponse(HttpResponseMessage response)
    {
        if (response is null)
            return false;

        HttpStatusCode code = response.StatusCode;

        if (code == HttpStatusCode.RequestTimeout)
            return true;

        if (code == HttpStatusCode.TooManyRequests)
            return true;

        return (int)code >= 500 && (int)code < 600;
    }
}
