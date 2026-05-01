using ArchLucid.Core.Diagnostics;

namespace ArchLucid.Api.Logging;

/// <summary>
///     Structured <see cref="ILogger" /> helpers for user-derived strings that must pass through
///     <see cref="LogSanitizer" /> (CWE-117).
/// </summary>
/// <remarks>
///     CodeQL <c>cs/log-forging</c> does not always propagate the custom <see cref="LogSanitizer.Sanitize" />
///     barrier model through <see cref="LoggerExtensions.LogWarning(ILogger, Exception?, string?, params object?[])" />
///     <c>params</c> boxing at controller call sites. Sanitizing in this helper keeps barrier and sink adjacent.
/// </remarks>
internal static class SanitizedLoggerExtensions
{
    /// <summary>Logs a warning with one placeholder filled from a user-derived string after sanitization.</summary>
    public static void LogWarningWithSanitizedUserArg(
        this ILogger logger,
        Exception? exception,
        string messageTemplate,
        string? userDerivedValue)
    {
        string safe = LogSanitizer.Sanitize(userDerivedValue);

        logger.LogWarning(exception, messageTemplate, safe); // codeql[cs/log-forging]: sanitized above; params boxing breaks custom barrier at sink (see CODEQL_TRIAGE.md, LoggerExtensions LogWarning boxing).
    }
}
