using ArchLucid.Contracts.Common;

using Microsoft.Extensions.Logging;

namespace ArchLucid.Core.Diagnostics;

/// <summary>
///     Structured <see cref="ILogger" /> helpers for Information-level messages that include multiple
///     user-derived strings (CWE-117).
/// </summary>
/// <remarks>
///     Each public wrapper sanitizes its user-derived <see cref="string" /> arguments through
///     <see cref="LogSanitizer.Sanitize(string?)" /> and then forwards them to a private
///     <see cref="LoggerMessageAttribute" />-generated emitter declared in the sibling partial file
///     <c>SanitizedLoggerInformationExtensions.LoggerMessage.cs</c>. The source generator emits
///     strongly-typed delegate calls instead of <c>params object?[]</c>; this preserves the custom
///     <c>LogSanitizer.Sanitize</c> barrier registered in the CodeQL model pack
///     (<c>.github/codeql/archlucid-csharp-log-sanitizer-models</c>) all the way to the
///     <see cref="ILogger.Log{TState}" /> sink and avoids the <c>cs/log-forging</c> false positives that
///     <see cref="LoggerExtensions.LogInformation(ILogger, string?, object?[])" /> boxing produces
///     (see <c>docs/library/CODEQL_TRIAGE.md</c>).
/// </remarks>
public static partial class SanitizedLoggerInformationExtensions
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

        EmitArchitectureRunCommitted(logger, safeRunId, safeManifestVersion, warningCount);
    }

    /// <summary>
    ///     Logs an idempotent commit path (manifest already stored — “committed” or “persisted at target version” retry), with
    ///     two user-derived strings sanitized.
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

        EmitCommitRunIdempotentReturn(logger, safeRunId, safeManifestVersion, traceCount);
    }

    /// <summary>
    ///     Logs a successful governance manifest promotion with four user-derived string placeholders sanitized before the sink.
    /// </summary>
    public static void LogInformationGovernanceManifestPromoted(
        this ILogger logger,
        string promotionRecordId,
        string runId,
        string manifestVersion,
        string targetEnvironment)
    {
        ArgumentNullException.ThrowIfNull(logger);

        string safePromotionRecordId = LogSanitizer.Sanitize(promotionRecordId);
        string safeRunId = LogSanitizer.Sanitize(runId);
        string safeManifestVersion = LogSanitizer.Sanitize(manifestVersion);
        string safeTargetEnvironment = LogSanitizer.Sanitize(targetEnvironment);

        EmitGovernanceManifestPromoted(
            logger,
            safePromotionRecordId,
            safeRunId,
            safeManifestVersion,
            safeTargetEnvironment);
    }

    /// <summary>
    ///     Logs successful governance environment activation with four user-derived string placeholders sanitized before the sink.
    /// </summary>
    public static void LogInformationGovernanceEnvironmentActivated(
        this ILogger logger,
        string activationId,
        string runId,
        string manifestVersion,
        string environment)
    {
        ArgumentNullException.ThrowIfNull(logger);

        string safeActivationId = LogSanitizer.Sanitize(activationId);
        string safeRunId = LogSanitizer.Sanitize(runId);
        string safeManifestVersion = LogSanitizer.Sanitize(manifestVersion);
        string safeEnvironment = LogSanitizer.Sanitize(environment);

        EmitGovernanceEnvironmentActivated(
            logger,
            safeActivationId,
            safeRunId,
            safeManifestVersion,
            safeEnvironment);
    }

    /// <summary>
    ///     Logs a successful comparison replay with four externally influenced string placeholders sanitized before the sink.
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

        EmitComparisonReplaySucceeded(
            logger,
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
    ///     Logs the start of an agent execution batch (run id + joined dispatch keys + task count), with user-derived strings
    ///     sanitized.
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

        EmitAgentExecutionBatchStarting(logger, safeRunId, taskCount, safeAgentTypeKeys);
    }

    /// <summary>Logs completion of an agent execution batch with a sanitized run id and result count.</summary>
    public static void LogInformationAgentExecutionBatchCompleted(this ILogger logger, string userRunId,
        int resultCount)
    {
        ArgumentNullException.ThrowIfNull(logger);

        string safeRunId = LogSanitizer.Sanitize(userRunId);

        EmitAgentExecutionBatchCompleted(logger, safeRunId, resultCount);
    }

    /// <summary>
    ///     Logs successful agent result submission (run id + result id from request body, status from persistence), with
    ///     user-derived strings sanitized.
    /// </summary>
    public static void LogInformationAgentResultSubmitted(
        this ILogger logger,
        string userRunId,
        string userResultId,
        AgentType agentType,
        ArchitectureRunStatus newStatus)
    {
        ArgumentNullException.ThrowIfNull(logger);

        string safeRunId = LogSanitizer.Sanitize(userRunId);
        string safeResultId = LogSanitizer.Sanitize(userResultId);

        EmitAgentResultSubmitted(logger, safeRunId, safeResultId, agentType, newStatus);
    }

    /// <summary>
    ///     Logs architecture run creation coordination success with four user-derived string placeholders sanitized before the sink.
    /// </summary>
    public static void LogInformationCreatingArchitectureRun(
        this ILogger logger,
        string userRunId,
        string userRequestId,
        string userSystemName,
        string userEnvironment)
    {
        ArgumentNullException.ThrowIfNull(logger);

        string safeRunId = LogSanitizer.Sanitize(userRunId);
        string safeRequestId = LogSanitizer.Sanitize(userRequestId);
        string safeSystemName = LogSanitizer.Sanitize(userSystemName);
        string safeEnvironment = LogSanitizer.Sanitize(userEnvironment);

        EmitCreatingArchitectureRun(
            logger,
            safeRunId,
            safeRequestId,
            safeSystemName,
            safeEnvironment);
    }
}
