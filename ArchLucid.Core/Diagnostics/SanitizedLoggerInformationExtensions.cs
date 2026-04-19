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
    /// Logs an idempotent commit path (manifest already stored — “committed” or “persisted at target version” retry), with two user-derived strings sanitized.
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

    /// <summary>
    /// Logs a successful comparison replay with four externally influenced string placeholders sanitized before the sink.
    /// </summary>
    public static void LogInformationComparisonReplaySucceeded(
        this ILogger logger,
        string? comparisonRecordId,
        string? comparisonType,
        string? format,
        string? replayMode,
        bool persistReplay,
        bool metadataOnly,
        long durationMs,
        bool verificationPassed)
    {
        ArgumentNullException.ThrowIfNull(logger);

        string safeComparisonRecordId = LogSanitizer.Sanitize(comparisonRecordId);
        string safeComparisonType = LogSanitizer.Sanitize(comparisonType);
        string safeFormat = LogSanitizer.Sanitize(format);
        string safeReplayMode = LogSanitizer.Sanitize(replayMode);

        // codeql[cs/log-forging]: string placeholders sanitized immediately above; bool/long args cannot inject log lines.
        logger.LogInformation(
            "Comparison replay: ComparisonRecordId={ComparisonRecordId}, Type={ComparisonType}, Format={Format}, ReplayMode={ReplayMode}, PersistReplay={PersistReplay}, MetadataOnly={MetadataOnly}, DurationMs={DurationMs}, VerificationPassed={VerificationPassed}",
            safeComparisonRecordId,
            safeComparisonType,
            safeFormat,
            safeReplayMode,
            persistReplay,
            metadataOnly,
            durationMs,
            verificationPassed);
    }

    /// <summary>
    /// Logs the start of an agent execution batch (run id + joined dispatch keys + task count), with user-derived strings sanitized.
    /// </summary>
    public static void LogInformationAgentExecutionBatchStarting(
        this ILogger logger,
        string userRunId,
        string userAgentTypeKeysJoined,
        int taskCount)
    {
        ArgumentNullException.ThrowIfNull(logger);

        string safeRunId = LogSanitizer.Sanitize(userRunId);
        string safeAgentTypeKeys = LogSanitizer.Sanitize(userAgentTypeKeysJoined);

        // codeql[cs/log-forging]: userRunId and joined agent type keys sanitized immediately above; taskCount is a value type.
        logger.LogInformation(
            "Agent execution batch starting: RunId={RunId}, TaskCount={TaskCount}, AgentTypeKeys={AgentTypeKeys}",
            safeRunId,
            taskCount,
            safeAgentTypeKeys);
    }

    /// <summary>Logs completion of an agent execution batch with a sanitized run id and result count.</summary>
    public static void LogInformationAgentExecutionBatchCompleted(this ILogger logger, string userRunId, int resultCount)
    {
        ArgumentNullException.ThrowIfNull(logger);

        string safeRunId = LogSanitizer.Sanitize(userRunId);

        // codeql[cs/log-forging]: userRunId sanitized immediately above; resultCount is a value type.
        logger.LogInformation(
            "Agent execution batch completed: RunId={RunId}, ResultCount={ResultCount}",
            safeRunId,
            resultCount);
    }
}
