using Microsoft.Extensions.Logging;

namespace ArchLucid.Core.Diagnostics;

/// <summary>
/// Structured <see cref="ILogger"/> helpers for Information-level messages that include multiple
/// user-derived strings (CWE-117).
/// </summary>
/// <remarks>
/// CodeQL <c>cs/log-forging</c> may not propagate the custom <see cref="LogSanitizer.Sanitize"/> barrier
/// through <see cref="LoggerExtensions.LogInformation(ILogger, string?, params object?[])"/> <c>params</c>
/// boxing at call sites. Sanitizing in this helper keeps barrier and sink adjacent (see <c>docs/CODEQL_TRIAGE.md</c>).
/// </remarks>
public static class SanitizedLoggerInformationExtensions
{
    /// <summary>Logs that an architecture run was committed, with two user-derived strings sanitized.</summary>
    public static void LogInformationArchitectureRunCommitted(
        this ILogger logger,
        string userRunId,
        string userManifestVersion,
        int warningCount)
    {
        ArgumentNullException.ThrowIfNull(logger);

        string safeRunId = LogSanitizer.Sanitize(userRunId);
        string safeManifestVersion = LogSanitizer.Sanitize(userManifestVersion);

        // codeql[cs/log-forging]: userRunId and userManifestVersion sanitized immediately above; warningCount is a value type.
        logger.LogInformation(
            "Architecture run committed: RunId={RunId}, ManifestVersion={ManifestVersion}, WarningCount={WarningCount}",
            safeRunId,
            safeManifestVersion,
            warningCount);
    }

    /// <summary>
    /// Logs an idempotent commit path (existing manifest returned), with two user-derived strings sanitized.
    /// </summary>
    public static void LogInformationCommitRunIdempotentReturn(
        this ILogger logger,
        string userRunId,
        string userManifestVersion,
        int traceCount)
    {
        ArgumentNullException.ThrowIfNull(logger);

        string safeRunId = LogSanitizer.Sanitize(userRunId);
        string safeManifestVersion = LogSanitizer.Sanitize(userManifestVersion);

        // codeql[cs/log-forging]: userRunId and userManifestVersion sanitized immediately above; traceCount is a value type.
        logger.LogInformation(
            "CommitRunAsync is idempotent: returning existing manifest for RunId={RunId}, ManifestVersion={ManifestVersion}, TraceCount={TraceCount}",
            safeRunId,
            safeManifestVersion,
            traceCount);
    }
}
