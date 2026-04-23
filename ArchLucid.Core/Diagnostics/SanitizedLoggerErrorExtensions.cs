using Microsoft.Extensions.Logging;

namespace ArchLucid.Core.Diagnostics;

/// <summary>
///     Structured <see cref="ILogger" /> helpers for Error-level messages that include request-derived strings (CWE-117).
/// </summary>
/// <remarks>
///     CodeQL <c>cs/log-forging</c> may not propagate <see cref="LogSanitizer.Sanitize" /> through
///     <see cref="LoggerExtensions.LogError(ILogger, Exception?, string?, object?[])" /> <c>params</c> boxing at call
///     sites.
///     Sanitizing in this helper keeps barrier and sink adjacent (see <c>docs/CODEQL_TRIAGE.md</c>).
/// </remarks>
public static class SanitizedLoggerErrorExtensions
{
    /// <summary>
    ///     Logs an unhandled exception with HTTP method and path after sanitization (worker / minimal HTTP hosts).
    /// </summary>
    public static void LogErrorUnhandledWorkerHttpRequest(
        this ILogger logger,
        Exception ex,
        string? requestMethod,
        string? requestPath)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(ex);

        string safeMethod = LogSanitizer.Sanitize(requestMethod);
        string safePath = LogSanitizer.Sanitize(requestPath);

        logger.LogError(
            ex,
            "Unhandled exception for {Method} {Path}",
            safeMethod,
            safePath); // codeql[cs/log-forging]: method and path sanitized immediately above.
    }
}
