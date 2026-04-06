namespace ArchiForge.Contracts.Common;

/// <summary>Lifecycle state of an individual <c>AgentTask</c> within a run.</summary>
public enum AgentTaskStatus
{
    /// <summary>Task created and queued for dispatch.</summary>
    Created = 1,
    /// <summary>Task dispatched to an agent and currently executing.</summary>
    InProgress = 2,
    /// <summary>Agent submitted a valid result; task complete.</summary>
    Completed = 3,
    /// <summary>Agent submitted a result that was rejected by validation.</summary>
    Rejected = 4,
    /// <summary>Task failed during execution; see task error details.</summary>
    Failed = 5
}
