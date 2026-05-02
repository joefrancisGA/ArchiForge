namespace ArchLucid.AgentRuntime.Evaluation;

/// <summary>
///     Thrown by <see cref="AgentOutputEvaluationRecorder" /> when
///     <c>AgentOutputQualityGateOptions.EnforceOnReject</c> is <c>true</c> and at least one agent trace
///     scores below the configured reject thresholds.  Callers that want to block manifest commit on
///     quality failures should catch this exception in the post-execute pipeline hook.
/// </summary>
public sealed class AgentOutputQualityGateRejectedException : Exception
{
    public AgentOutputQualityGateRejectedException(string runId, string traceId, string agentLabel)
        : base(
            $"Agent output quality gate rejected trace '{traceId}' (agent={agentLabel}) for run '{runId}'. " +
            "Structural or semantic score is below the configured reject threshold. " +
            "Disable AgentOutputQualityGateOptions.EnforceOnReject to revert to metrics-only mode.")
    {
        RunId = runId;
        TraceId = traceId;
        AgentLabel = agentLabel;
    }

    public string RunId { get; }
    public string TraceId { get; }
    public string AgentLabel { get; }
}
