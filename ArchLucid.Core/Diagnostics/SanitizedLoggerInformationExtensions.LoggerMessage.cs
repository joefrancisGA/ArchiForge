using ArchLucid.Contracts.Common;

using Microsoft.Extensions.Logging;

namespace ArchLucid.Core.Diagnostics;

/// <summary>
///     Source-generated <see cref="ILogger" /> emitters for
///     <see cref="SanitizedLoggerInformationExtensions" />.
/// </summary>
/// <remarks>
///     The <see cref="LoggerMessageAttribute" /> generator emits cached, strongly-typed delegates
///     instead of the <c>params object?[]</c> overload of <see cref="LoggerExtensions" />. That keeps
///     the dataflow from sanitized argument to <see cref="ILogger.Log{TState}" /> direct, which lets
///     the CodeQL <c>cs/log-forging</c> barrier registered for
///     <see cref="LogSanitizer.Sanitize(string?)" /> propagate to the sink (params boxing on the
///     classic overload defeats it — see <c>docs/library/CODEQL_TRIAGE.md</c>).
///     EventIds use the 3000 series, reserved for <c>ArchLucid.Core.Diagnostics</c> sanitized log
///     emitters; do not collide with the 1xxx (Decisioning) and 2xxx (Api Controllers) ranges already
///     in use.
/// </remarks>
public static partial class SanitizedLoggerInformationExtensions
{
    [LoggerMessage(
        EventId = 3001,
        Level = LogLevel.Information,
        Message =
            "Architecture run committed: RunId={RunId}, ManifestVersion={ManifestVersion}, WarningCount={WarningCount}")]
    private static partial void EmitArchitectureRunCommitted(
        ILogger logger,
        string runId,
        string manifestVersion,
        int warningCount);

    [LoggerMessage(
        EventId = 3002,
        Level = LogLevel.Information,
        Message =
            "CommitRunAsync is idempotent: returning existing manifest for RunId={RunId}, ManifestVersion={ManifestVersion}, TraceCount={TraceCount}")]
    private static partial void EmitCommitRunIdempotentReturn(
        ILogger logger,
        string runId,
        string manifestVersion,
        int traceCount);

    [LoggerMessage(
        EventId = 3003,
        Level = LogLevel.Information,
        Message =
            "Manifest promoted: PromotionRecordId={PromotionRecordId}, RunId={RunId}, ManifestVersion={ManifestVersion}, Target={TargetEnvironment}")]
    private static partial void EmitGovernanceManifestPromoted(
        ILogger logger,
        string promotionRecordId,
        string runId,
        string manifestVersion,
        string targetEnvironment);

    [LoggerMessage(
        EventId = 3004,
        Level = LogLevel.Information,
        Message =
            "Environment activated: ActivationId={ActivationId}, RunId={RunId}, ManifestVersion={ManifestVersion}, Environment={Environment}")]
    private static partial void EmitGovernanceEnvironmentActivated(
        ILogger logger,
        string activationId,
        string runId,
        string manifestVersion,
        string environment);

    [LoggerMessage(
        EventId = 3005,
        Level = LogLevel.Information,
        Message =
            "Comparison replay: ComparisonRecordId={ComparisonRecordId}, Type={ComparisonType}, Format={Format}, ReplayMode={ReplayMode}, PersistReplay={PersistReplay}, MetadataOnly={MetadataOnly}, DurationMs={DurationMs}, VerificationPassed={VerificationPassed}")]
    private static partial void EmitComparisonReplaySucceeded(
        ILogger logger,
        string comparisonRecordId,
        string comparisonType,
        string format,
        string replayMode,
        bool persistReplay,
        bool metadataOnly,
        long durationMs,
        bool verificationPassed);

    [LoggerMessage(
        EventId = 3006,
        Level = LogLevel.Information,
        Message =
            "Agent execution batch starting: RunId={RunId}, TaskCount={TaskCount}, AgentTypeKeys={AgentTypeKeys}")]
    private static partial void EmitAgentExecutionBatchStarting(
        ILogger logger,
        string runId,
        int taskCount,
        string agentTypeKeys);

    [LoggerMessage(
        EventId = 3007,
        Level = LogLevel.Information,
        Message = "Agent execution batch completed: RunId={RunId}, ResultCount={ResultCount}")]
    private static partial void EmitAgentExecutionBatchCompleted(
        ILogger logger,
        string runId,
        int resultCount);

    [LoggerMessage(
        EventId = 3008,
        Level = LogLevel.Information,
        Message =
            "Agent result submitted: RunId={RunId}, ResultId={ResultId}, AgentType={AgentType}, NewStatus={NewStatus}")]
    private static partial void EmitAgentResultSubmitted(
        ILogger logger,
        string runId,
        string resultId,
        AgentType agentType,
        ArchitectureRunStatus newStatus);

    [LoggerMessage(
        EventId = 3009,
        Level = LogLevel.Information,
        Message =
            "Creating architecture run: RunId={RunId}, RequestId={RequestId}, SystemName={SystemName}, Environment={Environment}")]
    private static partial void EmitCreatingArchitectureRun(
        ILogger logger,
        string runId,
        string requestId,
        string systemName,
        string environment);
}
