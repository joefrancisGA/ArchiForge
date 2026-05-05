using Microsoft.Extensions.Logging;

namespace ArchLucid.Core.Diagnostics;

/// <summary>
///     Structured <see cref="ILogger" /> helpers for SQL host leader lease coordination (CWE-117 and CodeQL exposure noise).
/// </summary>
/// <remarks>
///     Lease names and instance identifiers are scrubbed with <see cref="LogSanitizer" /> and logged only through this type
///     so <c>cs/log-forging</c> and <c>cs/exposure-of-sensitive-information</c> suppressions stay in one place (see
///     <c>docs/library/CODEQL_TRIAGE.md</c>). Static methods (not <c>this ILogger</c> extensions) avoid CodeQL treating raw
///     lease strings as <c>LogMessageSink</c> at coordinator call sites before sanitization runs.
/// </remarks>
public static class SanitizedLoggerHostLeaderElectionExtensions
{
    /// <summary>Debug: follower did not hold the lease and will wait before retrying.</summary>
    public static void LogDebugHostLeaderLeaseNotHeldFollowerWait(
        ILogger logger,
        string leaseName,
        int followerWaitMilliseconds)
    {
        ArgumentNullException.ThrowIfNull(logger);

        string safeLease = LogSanitizer.Sanitize(leaseName);

        // codeql[cs/log-forging]: lease name sanitized immediately above; followerWaitMilliseconds is value-typed.
        // codeql[cs/exposure-of-sensitive-information]: operational lease keys only; sanitized; not credentials (docs/library/CODEQL_TRIAGE.md).
        logger.LogDebug(
            "Host leader lease not held for {LeaseName}; follower wait {Ms} ms.",
            safeLease,
            followerWaitMilliseconds);
    }

    /// <summary>Lease acquired for this instance; operational telemetry only.</summary>
    public static void LogInformationHostLeaderLeaseAcquired(
        ILogger logger,
        string leaseName,
        string instanceId)
    {
        ArgumentNullException.ThrowIfNull(logger);

        string safeLease = LogSanitizer.Sanitize(leaseName);
        string safeInstance = LogSanitizer.Sanitize(instanceId);

        // codeql[cs/log-forging]: both placeholders sanitized immediately above.
        // codeql[cs/exposure-of-sensitive-information]: operational lease and instance telemetry; sanitized; non-credential.
        logger.LogInformation(
            "Acquired host leader lease {LeaseName} for instance {InstanceId}.",
            safeLease,
            safeInstance);
    }

    /// <summary>Leader work ended because lease was lost or handed off.</summary>
    public static void LogInformationHostLeaderWorkStoppedLeaseLossOrHandoff(
        ILogger logger,
        string leaseName)
    {
        ArgumentNullException.ThrowIfNull(logger);

        string safeLease = LogSanitizer.Sanitize(leaseName);

        // codeql[cs/log-forging]: lease name sanitized immediately above.
        // codeql[cs/exposure-of-sensitive-information]: operational lease key only; sanitized; not credentials.
        logger.LogInformation(
            "Leader work for {LeaseName} stopped after lease loss or handoff.",
            safeLease);
    }

    /// <summary>Renewal loop failed to extend the lease; leader work should stop.</summary>
    public static void LogWarningHostLeaderLeaseRenewalFailedStopping(
        ILogger logger,
        string leaseName,
        string instanceId)
    {
        ArgumentNullException.ThrowIfNull(logger);

        string safeLease = LogSanitizer.Sanitize(leaseName);
        string safeInstance = LogSanitizer.Sanitize(instanceId);

        // codeql[cs/log-forging]: both placeholders sanitized immediately above.
        // codeql[cs/exposure-of-sensitive-information]: operational lease and instance telemetry; sanitized; non-credential.
        logger.LogWarning(
            "Failed to renew host leader lease {LeaseName} for {InstanceId}; stopping leader work.",
            safeLease,
            safeInstance);
    }
}
