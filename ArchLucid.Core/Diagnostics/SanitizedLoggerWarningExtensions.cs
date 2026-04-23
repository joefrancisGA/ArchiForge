using Microsoft.Extensions.Logging;

namespace ArchLucid.Core.Diagnostics;

/// <summary>
///     Structured <see cref="ILogger" /> helpers for Warning-level messages that include two or three
///     externally influenced strings (CWE-117).
/// </summary>
/// <remarks>
///     CodeQL <c>cs/log-forging</c> may not propagate the custom <see cref="LogSanitizer.Sanitize" /> barrier
///     through <see cref="LoggerExtensions.LogWarning(ILogger, string?, params object?[])" /> <c>params</c>
///     boxing at call sites. Sanitizing in this helper keeps barrier and sink adjacent (see <c>docs/CODEQL_TRIAGE.md</c>).
/// </remarks>
public static class SanitizedLoggerWarningExtensions
{
    /// <summary>
    ///     Logs a warning whose template has two placeholders filled from externally influenced strings after sanitization.
    /// </summary>
    public static void LogWarningWithTwoSanitizedUserStrings(
        this ILogger logger,
        string messageTemplate,
        string? userDerivedFirst,
        string? userDerivedSecond)
    {
        ArgumentNullException.ThrowIfNull(logger);

        string safeFirst = LogSanitizer.Sanitize(userDerivedFirst);
        string safeSecond = LogSanitizer.Sanitize(userDerivedSecond);

        // codeql[cs/log-forging]: user-derived values sanitized immediately above.
        logger.LogWarning(messageTemplate, safeFirst, safeSecond);
    }

    /// <summary>
    ///     Logs a warning whose template has three placeholders filled from externally influenced strings after sanitization.
    /// </summary>
    public static void LogWarningWithThreeSanitizedUserStrings(
        this ILogger logger,
        string messageTemplate,
        string? userDerivedFirst,
        string? userDerivedSecond,
        string? userDerivedThird)
    {
        ArgumentNullException.ThrowIfNull(logger);

        string safeFirst = LogSanitizer.Sanitize(userDerivedFirst);
        string safeSecond = LogSanitizer.Sanitize(userDerivedSecond);
        string safeThird = LogSanitizer.Sanitize(userDerivedThird);

        // codeql[cs/log-forging]: user-derived values sanitized immediately above.
        logger.LogWarning(messageTemplate, safeFirst, safeSecond, safeThird);
    }

    /// <summary>
    ///     Logs comparison replay failure with an exception and user-derived strings after sanitization.
    /// </summary>
    /// <remarks>
    ///     <see cref="InvalidOperationException.Message" /> / <see cref="Exception.Message" /> and comparison record ids
    ///     can include control characters; booleans cannot (CWE-117 is string-sink concern). Call this instead of
    ///     <see cref="LoggerExtensions.LogWarning(ILogger, Exception?, string?, object?[])" /> at the replay API boundary
    ///     so CodeQL sees the sanitizer adjacent to the sink (see <c>docs/CODEQL_TRIAGE.md</c>).
    /// </remarks>
    public static void LogWarningComparisonReplayFailed(
        this ILogger logger,
        Exception ex,
        string? comparisonRecordId,
        bool notFound,
        bool metadataOnly,
        string? errorMessage)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(ex);

        string safeComparisonRecordId = LogSanitizer.Sanitize(comparisonRecordId);
        string safeErrorMessage = LogSanitizer.Sanitize(errorMessage);

        logger.LogWarning(
            ex,
            "Comparison replay failed: ComparisonRecordId={ComparisonRecordId}, NotFound={NotFound}, MetadataOnly={MetadataOnly}, Error={Error}",
            safeComparisonRecordId,
            notFound,
            metadataOnly,
            safeErrorMessage); // codeql[cs/log-forging]: strings sanitized immediately above; bools cannot inject log lines.
    }
}
