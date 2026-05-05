using System.Net;

using Microsoft.Extensions.Logging;

using Polly;
using Polly.Retry;

namespace ArchLucid.Application.Governance;

/// <summary>
///     Polly outbound retry for governance SLA escalation POSTs: exponential backoff, <see cref="HttpRequestException" />,
///     HTTP 5xx, and 429.
/// </summary>
internal static class GovernanceSlaEscalationWebhookRetryPipeline
{
    /// <summary>Retries after the first attempt (so up to four HTTP tries when set to three).</summary>
    internal const int MaxRetryAttempts = 3;

    internal static ResiliencePipeline<HttpResponseMessage> Create(
        ILogger logger,
        string sanitizedApprovalRequestLabel) =>
        Create(logger, sanitizedApprovalRequestLabel, ProductionBackoffProvider);

    /// <remarks>
    ///     The default <paramref name="retryBackoff" /> uses exponential seconds (2^attempt); tests typically pass an
    ///     argument that yields <see cref="TimeSpan.Zero" /> to avoid sleeping the test host.
    /// </remarks>
    internal static ResiliencePipeline<HttpResponseMessage> Create(
        ILogger logger,
        string sanitizedApprovalRequestLabel,
        Func<int, TimeSpan> retryBackoff)
    {
        ArgumentNullException.ThrowIfNull(logger);

        ArgumentNullException.ThrowIfNull(retryBackoff);

        return new ResiliencePipelineBuilder<HttpResponseMessage>()
            .AddRetry(new RetryStrategyOptions<HttpResponseMessage>
            {
                MaxRetryAttempts = MaxRetryAttempts,
                DelayGenerator = args =>
                {
                    TimeSpan delay = retryBackoff(args.AttemptNumber);

                    return new ValueTask<TimeSpan?>(delay);
                },
                ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                    .Handle<HttpRequestException>()
                    .HandleResult(static r => ShouldRetryStatus(r)),
                OnRetry = args =>
                {
                    TryLogRetry(logger, sanitizedApprovalRequestLabel, args);

                    return ValueTask.CompletedTask;
                },
            })
            .Build();
    }

    private static TimeSpan ProductionBackoffProvider(int attemptNumber) =>
        TimeSpan.FromSeconds(Math.Pow(2, attemptNumber));

    private static void TryLogRetry(
        ILogger logger,
        string sanitizedApprovalRequestLabel,
        OnRetryArguments<HttpResponseMessage> args)
    {
        if (!logger.IsEnabled(LogLevel.Warning))

            return;

        if (args.Outcome.Result is { } response)
        {
            HttpStatusCode code = response.StatusCode;
            response.Dispose();

            logger.LogWarning(
                "SLA escalation webhook scheduling retry after HTTP {StatusCode}; attempt {RetryAttempt} of {MaxRetries} (next delay {RetryDelay}). ApprovalRequestId={ApprovalRequestId}.",
                (int)code,
                args.AttemptNumber,
                MaxRetryAttempts,
                args.RetryDelay,
                sanitizedApprovalRequestLabel);

            return;
        }

        if (args.Outcome.Exception is { } ex)

            logger.LogWarning(
                ex,
                "SLA escalation webhook scheduling retry after transport error; attempt {RetryAttempt} of {MaxRetries} (next delay {RetryDelay}). ApprovalRequestId={ApprovalRequestId}.",
                args.AttemptNumber,
                MaxRetryAttempts,
                args.RetryDelay,
                sanitizedApprovalRequestLabel);
    }

    private static bool ShouldRetryStatus(HttpResponseMessage response)
    {
        HttpStatusCode code = response.StatusCode;

        if (code == HttpStatusCode.TooManyRequests)

            return true;

        return (int)code >= 500 && (int)code < 600;
    }
}
