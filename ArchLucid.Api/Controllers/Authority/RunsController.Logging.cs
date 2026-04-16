namespace ArchLucid.Api.Controllers;

/// <summary>Source-generated structured log methods for <see cref="RunsController"/>.</summary>
public sealed partial class RunsController
{
    [LoggerMessage(EventId = 2001, Level = LogLevel.Information, Message = "Run created: RunId={RunId}, RequestId={RequestId}, User={User}, CorrelationId={CorrelationId}")]
    private partial void LogRunCreated(string runId, string requestId, string user, string correlationId);

    [LoggerMessage(EventId = 2002, Level = LogLevel.Information, Message = "Run executed: RunId={RunId}, ResultCount={ResultCount}, User={User}, CorrelationId={CorrelationId}")]
    private partial void LogRunExecuted(string runId, int resultCount, string user, string correlationId);

    [LoggerMessage(EventId = 2003, Level = LogLevel.Information, Message = "Run replayed: OriginalRunId={OriginalRunId}, ReplayRunId={ReplayRunId}, ExecutionMode={ExecutionMode}, User={User}, CorrelationId={CorrelationId}")]
    private partial void LogRunReplayed(string originalRunId, string replayRunId, string executionMode, string user, string correlationId);

    [LoggerMessage(EventId = 2004, Level = LogLevel.Information, Message = "Run committed: RunId={RunId}, ManifestVersion={ManifestVersion}, WarningCount={WarningCount}, User={User}, CorrelationId={CorrelationId}")]
    private partial void LogRunCommitted(string runId, string? manifestVersion, int warningCount, string user, string correlationId);

    [LoggerMessage(EventId = 2005, Level = LogLevel.Information, Message = "Fake results seeded: RunId={RunId}, ResultCount={ResultCount}, User={User}, CorrelationId={CorrelationId}")]
    private partial void LogFakeResultsSeeded(string runId, int resultCount, string user, string correlationId);
}
