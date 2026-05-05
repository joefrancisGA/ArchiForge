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
    ///     Logs a warning with one placeholder filled from an externally influenced string after sanitization (CWE-117).
    /// </summary>
    /// <remarks>
    ///     Prefer this over <see cref="LoggerExtensions.LogWarning(ILogger, Exception?, string?, object?[])" /> at API boundaries:
    ///     CodeQL <c>cs/log-forging</c> may not propagate <see cref="LogSanitizer.Sanitize" /> through
    ///     <c>params object?[]</c> boxing when the template and exception are assembled at the call site.
    /// </remarks>
    public static void LogWarningWithSanitizedUserArg(
        this ILogger logger,
        Exception? exception,
        string messageTemplate,
        string? userDerivedValue)
    {
        ArgumentNullException.ThrowIfNull(logger);

        string safe = LogSanitizer.Sanitize(userDerivedValue);

        logger.LogWarning(
            exception,
            messageTemplate,
            safe); // codeql[cs/log-forging]: sanitized immediately above; exception + params boxing breaks custom barrier at API call sites.
    }

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

        logger.LogWarning(messageTemplate, safeFirst, safeSecond); // codeql[cs/log-forging]: user-derived values sanitized immediately above (params boxing).
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

        logger.LogWarning(messageTemplate, safeFirst, safeSecond, safeThird); // codeql[cs/log-forging]: user-derived values sanitized immediately above (params boxing).
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

        // codeql[cs/log-forging]: ComparisonRecordId and Error placeholders sanitized immediately above (params boxing breaks LogSanitizer barrier at downstream call sites). NotFound and MetadataOnly are booleans (no CRLF injection). Full exception forwarded for structured telemetry sinks; CWE-117 string sink uses only sanitized placeholders (docs/library/CODEQL_TRIAGE.md).
        logger.LogWarning(
            ex,
            "Comparison replay failed: ComparisonRecordId={ComparisonRecordId}, NotFound={NotFound}, MetadataOnly={MetadataOnly}, Error={Error}",
            safeComparisonRecordId,
            notFound,
            metadataOnly,
            safeErrorMessage);
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

    /// <summary>
    ///     Service Bus integration publish failure: <paramref name="eventType" /> is a canonical urn from
    ///     <see cref="ArchLucid.Core.Integration.IntegrationEventTypes" /> (externally influenced at the API boundary).
    /// </summary>
    /// <remarks>
    ///     Prefer this over raw <see cref="LoggerExtensions.LogWarning(ILogger, Exception?, string?, object?[])" /> so
    ///     <see cref="LogSanitizer" /> stays adjacent to the sink and operational event-type logging is centralized (see
    ///     <c>docs/library/CODEQL_TRIAGE.md</c> coordinator / integration event notes).
    /// </remarks>
    public static void LogWarningIntegrationEventServiceBusPublishFailed(
        this ILogger logger,
        Exception ex,
        string? eventType)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(ex);

        string safeEventType = LogSanitizer.Sanitize(eventType);

        // codeql[cs/log-forging]: integration event type string sanitized immediately above.
        // codeql[cs/exposure-of-sensitive-information]: canonical event-type urn taxonomy only; sanitized; not credentials or subscriber PII (docs/library/CODEQL_TRIAGE.md).
        logger.LogWarning(
            ex,
            "Failed to publish integration event type {EventType} to Service Bus.",
            safeEventType);
    }
}
