using Microsoft.Extensions.Logging;

namespace ArchLucid.Core.Audit;

/// <summary>
///     Bounded retries for durable SQL audit writes on security-relevant paths where a single transient failure
///     should not silently drop the audit row.
/// </summary>
public static class DurableAuditLogRetry
{
    /// <summary>
    ///     Runs <paramref name="writeAsync" /> up to <paramref name="maxAttempts" /> times with short backoff.
    ///     Logs and suppresses the final exception so callers keep their non-audit behavior.
    /// </summary>
    public static async Task TryLogAsync(
        Func<CancellationToken, Task> writeAsync,
        ILogger logger,
        string operationLabel,
        CancellationToken cancellationToken,
        int maxAttempts = 3)
    {
        ArgumentNullException.ThrowIfNull(writeAsync);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentException.ThrowIfNullOrWhiteSpace(operationLabel);

        if (maxAttempts < 1)
            throw new ArgumentOutOfRangeException(nameof(maxAttempts));


        Exception? last = null;

        for (int attempt = 1; attempt <= maxAttempts; attempt++)

            try
            {
                await writeAsync(cancellationToken);

                return;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                last = ex;

                if (logger.IsEnabled(LogLevel.Warning))

                    logger.LogWarning(
                        ex,
                        "Durable audit attempt {Attempt}/{MaxAttempts} failed for {OperationLabel}",
                        attempt,
                        maxAttempts,
                        operationLabel);


                if (attempt < maxAttempts)

                    await Task.Delay(TimeSpan.FromMilliseconds(50 * (1 << (attempt - 1))), cancellationToken);
            }


        if (last is not null && logger.IsEnabled(LogLevel.Warning))

            logger.LogWarning(
                last,
                "Durable audit abandoned after {MaxAttempts} attempts for {OperationLabel}",
                maxAttempts,
                operationLabel);
    }
}
