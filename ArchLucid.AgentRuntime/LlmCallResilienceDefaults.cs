using System.ClientModel;
using System.Diagnostics;
using System.Net;

using ArchLucid.Core.Diagnostics;
using ArchLucid.Core.Resilience;

using Microsoft.Extensions.Logging;

using Polly;
using Polly.Retry;

namespace ArchLucid.AgentRuntime;

/// <summary>Builds Polly retry pipelines for Azure OpenAI completion/embedding calls (inside circuit breaker decorators).</summary>
public static class LlmCallResilienceDefaults
{
    /// <summary>
    /// Retry transient LLM errors with exponential backoff and jitter before a failure is surfaced to the circuit breaker.
    /// </summary>
    /// <param name="logger">Optional logger for retry warnings.</param>
    /// <param name="maxRetryAttempts">Retry attempts after the first call; 0 disables retry (<see cref="ResiliencePipeline"/> no-op).</param>
    /// <param name="baseDelay">First backoff delay.</param>
    /// <param name="maxDelay">Cap for any single delay.</param>
    /// <param name="gateName">Optional circuit breaker gate name for metrics.</param>
    public static ResiliencePipeline BuildLlmRetryPipeline(
        ILogger? logger = null,
        int maxRetryAttempts = 3,
        TimeSpan? baseDelay = null,
        TimeSpan? maxDelay = null,
        string? gateName = null)
    {
        if (maxRetryAttempts <= 0)
        {
            return ResiliencePipeline.Empty;
        }

        TimeSpan delay = baseDelay ?? TimeSpan.FromMilliseconds(500);
        TimeSpan cap = maxDelay ?? TimeSpan.FromSeconds(10);

        return new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
                MaxRetryAttempts = maxRetryAttempts,
                Delay = delay,
                MaxDelay = cap,
                BackoffType = DelayBackoffType.Exponential,
                UseJitter = true,
                ShouldHandle = new PredicateBuilder().Handle<Exception>(ShouldRetryLlmException),
                OnRetry = args =>
                {
                    TagList metricTags = new TagList();

                    if (!string.IsNullOrEmpty(gateName))
                    {
                        metricTags.Add("gate", gateName);
                    }

                    metricTags.Add("attempt", args.AttemptNumber);
                    metricTags.Add(
                        "exception_type",
                        args.Outcome.Exception?.GetType().Name ?? "unknown");

                    ArchLucidInstrumentation.LlmCallRetries.Add(1, metricTags);

                    if (logger is not null && args.Outcome.Exception is Exception ex)
                    {
                        logger.LogWarning(
                            ex,
                            "Transient LLM error; retry {AttemptNumber}/{MaxRetryAttempts} after {RetryDelay}.",
                            args.AttemptNumber,
                            maxRetryAttempts,
                            args.RetryDelay);
                    }

                    return ValueTask.CompletedTask;
                },
            })
            .Build();
    }

    /// <summary>Used by chaos/retry composition tests (Simmy) aligned with the same classification rules.</summary>
    internal static bool ShouldRetryLlmException(Exception ex)
    {
        if (ex is OperationCanceledException oce && oce.CancellationToken.IsCancellationRequested)
        {
            return false;
        }

        if (ex is CircuitBreakerOpenException)
        {
            return false;
        }

        if (ex is InvalidOperationException)
        {
            return false;
        }

        if (ex is TaskCanceledException tce && !tce.CancellationToken.IsCancellationRequested)
        {
            return true;
        }

        if (ex is HttpRequestException hre)
        {
            if (hre.StatusCode is HttpStatusCode sc)
            {
                int code = (int)sc;

                return code is 429 or 500 or 502 or 503 or 504;
            }

            return true;
        }

        if (ex is ClientResultException cre)
        {
            int status = cre.Status;

            return status is 429 or 500 or 502 or 503 or 504;
        }

        return false;
    }
}
