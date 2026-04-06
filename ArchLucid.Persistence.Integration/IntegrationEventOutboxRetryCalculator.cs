namespace ArchiForge.Persistence.Integration;

/// <summary>Computes exponential backoff after a failed Service Bus publish.</summary>
public static class IntegrationEventOutboxRetryCalculator
{
    /// <param name="failureCountAfterIncrement">Value stored in <c>RetryCount</c> after this failure (1-based failures).</param>
    /// <param name="maxBackoffSeconds">Cap for delay (e.g. from options).</param>
    public static TimeSpan DelayUntilNextAttempt(int failureCountAfterIncrement, int maxBackoffSeconds)
    {
        if (failureCountAfterIncrement < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(failureCountAfterIncrement));
        }

        int cap = Math.Clamp(maxBackoffSeconds, 1, 86_400);
        double seconds = Math.Pow(2, failureCountAfterIncrement);

        if (seconds > cap)
        {
            return TimeSpan.FromSeconds(cap);
        }

        return TimeSpan.FromSeconds(seconds);
    }
}
