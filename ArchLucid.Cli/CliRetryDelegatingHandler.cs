using System.Net;
using System.Net.Http;

using Polly;
using Polly.Retry;

namespace ArchiForge.Cli;

/// <summary>Retries transient HTTP failures for CLI outbound calls (matches legacy <c>ArchiForgeApiClient</c> resilience).</summary>
internal sealed class CliRetryDelegatingHandler : DelegatingHandler
{
    private readonly ResiliencePipeline<HttpResponseMessage> _pipeline = new ResiliencePipelineBuilder<HttpResponseMessage>()
        .AddRetry(
            new RetryStrategyOptions<HttpResponseMessage>
            {
                MaxRetryAttempts = 3,
                Delay = TimeSpan.FromSeconds(1),
                BackoffType = DelayBackoffType.Exponential,
                UseJitter = true,
                ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                    .Handle<HttpRequestException>()
                    .HandleResult(
                        static r =>
                            (int)r.StatusCode >= 500
                            || r.StatusCode == HttpStatusCode.RequestTimeout
                            || r.StatusCode == (HttpStatusCode)429),
            })
        .Build();

    /// <inheritdoc />
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        return await _pipeline.ExecuteAsync(
                ct => new ValueTask<HttpResponseMessage>(base.SendAsync(request, ct)),
                cancellationToken)
            ;
    }
}
