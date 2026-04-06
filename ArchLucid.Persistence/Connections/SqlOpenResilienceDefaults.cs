using Microsoft.Extensions.Logging;

using Polly;
using Polly.Retry;

namespace ArchiForge.Persistence.Connections;

/// <summary>Builds <see cref="ResiliencePipeline"/> instances for SQL connection open retries (transient errors only).</summary>
public static class SqlOpenResilienceDefaults
{
    /// <summary>Matches the historical <see cref="ResilientSqlConnectionFactory"/> defaults: 3 attempts, 200 ms base exponential backoff with jitter.</summary>
    public static ResiliencePipeline BuildSqlOpenRetryPipeline(
        ILogger<ResilientSqlConnectionFactory>? logger = null,
        int maxRetryAttempts = 3,
        TimeSpan? baseDelay = null)
    {
        // Polly.Retry.RetryStrategyOptions.MaxRetryAttempts must be >= 1; callers use 0 to mean "no retries".
        if (maxRetryAttempts <= 0)
            return new ResiliencePipelineBuilder().Build();

        TimeSpan delay = baseDelay ?? TimeSpan.FromMilliseconds(200);

        return new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
                MaxRetryAttempts = maxRetryAttempts,
                Delay = delay,
                BackoffType = DelayBackoffType.Exponential,
                UseJitter = true,
                ShouldHandle = new PredicateBuilder().Handle<Exception>(ex => SqlTransientDetector.IsTransient(ex)),
                OnRetry = args =>
                {
                    if (logger is not null && args.Outcome.Exception is Exception ex)
                    {
                        logger.LogWarning(
                            ex,
                            "Transient SQL error on connection open; retry {AttemptNumber} (max {MaxRetryAttempts}).",
                            args.AttemptNumber,
                            maxRetryAttempts);
                    }

                    return ValueTask.CompletedTask;
                },
            })
            .Build();
    }
}
