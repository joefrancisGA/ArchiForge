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

    /// <summary>
    ///     Warns when architecture run execution fails (run id and exception type name sanitized — CWE-117).
    /// </summary>
    /// <remarks>
    ///     Prefer this over <see cref="LoggerExtensions.LogWarning(ILogger, Exception?, string?, object?[])" /> at the execute
    ///     boundary so CodeQL sees <see cref="LogSanitizer.Sanitize" /> adjacent to the sink (params boxing breaks custom barriers).
    /// </remarks>
    public static void LogWarningArchitectureRunExecutionFailed(
        this ILogger logger,
        Exception ex,
        string? runId,
        string? exceptionTypeName)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(ex);

        string safeRunId = LogSanitizer.Sanitize(runId);
        string safeExceptionType = LogSanitizer.Sanitize(exceptionTypeName);

        logger.LogWarning(
            ex,
            "Architecture run execution failed: RunId={RunId}, ExceptionType={ExceptionType}",
            safeRunId,
            safeExceptionType); // codeql[cs/log-forging]: strings sanitized immediately above.
    }

    /// <summary>
    ///     Warns when agent output structural completeness falls below product threshold (run/trace identifiers may be externally influenced — CWE-117).
    /// </summary>
    public static void LogWarningAgentOutputStructuralScoreBelowThreshold(
        this ILogger logger,
        double structuralCompletenessRatio,
        string? runId,
        string? traceId,
        string? agentLabel,
        int missingKeyCount)
    {
        ArgumentNullException.ThrowIfNull(logger);

        string safeRunId = LogSanitizer.Sanitize(runId);
        string safeTraceId = LogSanitizer.Sanitize(traceId);
        string safeAgent = LogSanitizer.Sanitize(agentLabel);

        logger.LogWarning(
            "Agent output structural score {Score:F2} below threshold for run {RunId} trace {TraceId} agent {AgentType}; missing key count {MissingCount}.",
            structuralCompletenessRatio,
            safeRunId,
            safeTraceId,
            safeAgent,
            missingKeyCount); // codeql[cs/log-forging]: string placeholders sanitized immediately above; score and MissingCount are value types.
    }

    /// <summary>
    ///     Warns when the agent output quality gate rejects scores for a trace.
    /// </summary>
    public static void LogWarningAgentOutputQualityGateRejected(
        this ILogger logger,
        string? runId,
        string? traceId,
        string? agentLabel,
        double structuralCompletenessRatio,
        double overallSemanticScore)
    {
        ArgumentNullException.ThrowIfNull(logger);

        string safeRunId = LogSanitizer.Sanitize(runId);
        string safeTraceId = LogSanitizer.Sanitize(traceId);
        string safeAgent = LogSanitizer.Sanitize(agentLabel);

        logger.LogWarning(
            "Agent output quality gate rejected run {RunId} trace {TraceId} agent {AgentType} (structural {Structural:F2}, semantic {Semantic:F2}).",
            safeRunId,
            safeTraceId,
            safeAgent,
            structuralCompletenessRatio,
            overallSemanticScore); // codeql[cs/log-forging]: string placeholders sanitized immediately above; floats cannot inject CRLF lines.
    }

    /// <summary>
    ///     Warns when the agent output quality gate warns (non-reject outcome) on a trace.
    /// </summary>
    public static void LogWarningAgentOutputQualityGateWarned(
        this ILogger logger,
        string? runId,
        string? traceId,
        string? agentLabel,
        double structuralCompletenessRatio,
        double overallSemanticScore)
    {
        ArgumentNullException.ThrowIfNull(logger);

        string safeRunId = LogSanitizer.Sanitize(runId);
        string safeTraceId = LogSanitizer.Sanitize(traceId);
        string safeAgent = LogSanitizer.Sanitize(agentLabel);

        logger.LogWarning(
            "Agent output quality gate warned for run {RunId} trace {TraceId} agent {AgentType} (structural {Structural:F2}, semantic {Semantic:F2}).",
            safeRunId,
            safeTraceId,
            safeAgent,
            structuralCompletenessRatio,
            overallSemanticScore); // codeql[cs/log-forging]: string placeholders sanitized immediately above; floats cannot inject CRLF lines.
    }

    /// <summary>
    ///     Warns when semantic score is critically low for a persisted agent trace (run/trace/agent labels sanitized).
    /// </summary>
    public static void LogWarningAgentOutputSemanticScoreBelowThreshold(
        this ILogger logger,
        double overallSemanticScore,
        string? runId,
        string? traceId,
        string? agentLabel,
        int emptyClaimCount,
        int incompleteFindingCount)
    {
        ArgumentNullException.ThrowIfNull(logger);

        string safeRunId = LogSanitizer.Sanitize(runId);
        string safeTraceId = LogSanitizer.Sanitize(traceId);
        string safeAgent = LogSanitizer.Sanitize(agentLabel);

        logger.LogWarning(
            "Agent output semantic score {Score:F2} below threshold for run {RunId} trace {TraceId} agent {AgentType}; empty claims {EmptyClaims}, incomplete findings {IncompleteFindings}.",
            overallSemanticScore,
            safeRunId,
            safeTraceId,
            safeAgent,
            emptyClaimCount,
            incompleteFindingCount); // codeql[cs/log-forging]: string placeholders sanitized immediately above; counts are value types.
    }

    /// <summary>
    ///     Operator-shell client error telemetry: all placeholders are externally influenced strings (CWE-117).
    /// </summary>
    /// <remarks>
    ///     Keeps <see cref="LogSanitizer.Sanitize" /> adjacent to the <see cref="ILogger" /> sink so
    ///     <c>cs/log-forging</c> does not trip on <c>params object?[]</c> boxing from controller call sites.
    /// </remarks>
    public static void LogWarningOperatorShellClientError(
        this ILogger logger,
        string? clientMessage,
        string? pathname,
        string? userAgent,
        string? timestampUtc,
        string? stackTrace)
    {
        ArgumentNullException.ThrowIfNull(logger);

        string safeMessage = LogSanitizer.Sanitize(clientMessage);
        string safePath = LogSanitizer.Sanitize(pathname);
        string safeUserAgent = LogSanitizer.Sanitize(userAgent);
        string safeTimestamp = LogSanitizer.Sanitize(timestampUtc);
        string safeStack = LogSanitizer.Sanitize(stackTrace);

        logger.LogWarning(
            "Operator shell client error: {ClientErrorMessage} | Path={ClientErrorPathname} | UA={ClientErrorUserAgent} | At={ClientErrorTimestamp} | Stack={ClientErrorStack}",
            safeMessage,
            safePath,
            safeUserAgent,
            safeTimestamp,
            safeStack); // codeql[cs/log-forging]: string placeholders sanitized immediately above.
    }
}
