using System.Net;

using Polly;
using Polly.Retry;

namespace ArchLucid.Cli;

/// <summary>Retries transient HTTP failures for CLI outbound calls (matches legacy <c>ArchLucidApiClient</c> resilience).</summary>
internal sealed class CliRetryDelegatingHandler : DelegatingHandler
{
    private readonly ResiliencePipeline<HttpResponseMessage>? _pipeline;

    public CliRetryDelegatingHandler(CliResilienceOptions? options = null)
    {
        CliResilienceOptions resolved = options ?? new CliResilienceOptions();
        resolved.Normalize();

        if (resolved.MaxRetryAttempts <= 0)
        {
            _pipeline = null;

            return;
        }

        _pipeline = new ResiliencePipelineBuilder<HttpResponseMessage>()
            .AddRetry(
                new RetryStrategyOptions<HttpResponseMessage>
                {
                    MaxRetryAttempts = resolved.MaxRetryAttempts,
                    Delay = TimeSpan.FromSeconds(resolved.InitialDelaySeconds),
                    BackoffType = DelayBackoffType.Exponential,
                    UseJitter = true,
                    ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                        .Handle<HttpRequestException>()
                        .HandleResult(static r =>
                            (int)r.StatusCode >= 500
                            || r.StatusCode == HttpStatusCode.RequestTimeout
                            || r.StatusCode == (HttpStatusCode)429)
                })
            .Build();
    }

    /// <inheritdoc />
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        if (_pipeline is null)
            return await base.SendAsync(request, cancellationToken);

        return await _pipeline.ExecuteAsync(
            ct => new ValueTask<HttpResponseMessage>(base.SendAsync(request, ct)),
            cancellationToken);
    }
}
