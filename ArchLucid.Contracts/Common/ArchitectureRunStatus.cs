namespace ArchiForge.Contracts.Common;

/// <summary>Lifecycle state of an <see cref="ArchiForge.Contracts.Metadata.ArchitectureRun"/>.</summary>
public enum ArchitectureRunStatus
{
    /// <summary>Run record created; no tasks generated yet.</summary>
    Created = 1,
    /// <summary>Agent tasks have been generated and are ready for dispatch.</summary>
    TasksGenerated = 2,
    /// <summary>Tasks dispatched; waiting for all agent results to be submitted.</summary>
    WaitingForResults = 3,
    /// <summary>All results received; run is ready to be committed to a golden manifest.</summary>
    ReadyForCommit = 4,
    /// <summary>Golden manifest committed; run is complete and immutable.</summary>
    Committed = 5,
    /// <summary>Run failed during task execution or commit; see run error details.</summary>
    Failed = 6
}
