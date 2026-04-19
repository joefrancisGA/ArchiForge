using Microsoft.Extensions.Logging;

namespace ArchLucid.Core.Diagnostics;

/// <summary>
/// Structured <see cref="ILogger"/> helpers for Debug-level messages with user-derived string placeholders (CWE-117).
/// </summary>
/// <remarks>
/// CodeQL <c>cs/log-forging</c> may not propagate <see cref="LogSanitizer.Sanitize"/> through
/// <see cref="LoggerExtensions.LogDebug(ILogger, string?, params object?[])"/> <c>params</c> boxing at call sites
/// (see <c>docs/CODEQL_TRIAGE.md</c>).
/// </remarks>
public static class SanitizedLoggerDebugExtensions
{
    /// <summary>Logs per-task agent handler completion with three user-derived strings sanitized.</summary>
    public static void LogDebugAgentTaskFinished(
        this ILogger logger,
        string userRunId,
        string userTaskId,
        string userAgentTypeKey,
        long durationMs)
    {
        ArgumentNullException.ThrowIfNull(logger);

        string safeRunId = LogSanitizer.Sanitize(userRunId);
        string safeTaskId = LogSanitizer.Sanitize(userTaskId);
        string safeAgentTypeKey = LogSanitizer.Sanitize(userAgentTypeKey);

        // codeql[cs/log-forging]: string placeholders sanitized immediately above; durationMs is a value type.
        logger.LogDebug(
            "Agent task finished: RunId={RunId}, TaskId={TaskId}, AgentTypeKey={AgentTypeKey}, DurationMs={DurationMs}",
            safeRunId,
            safeTaskId,
            safeAgentTypeKey,
            durationMs);
    }
}
