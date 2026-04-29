using ArchLucid.Contracts.Agents;
using ArchLucid.Contracts.DecisionTraces;
using ArchLucid.Contracts.Manifest;
using ArchLucid.Contracts.Metadata;

namespace ArchLucid.Application;

/// <summary>
/// Outcome of <see cref="Runs.Orchestration.IArchitectureRunCreateOrchestrator.CreateRunAsync"/> after successful coordination and persistence.
/// </summary>
public sealed class CreateRunResult
{
    /// <summary>Run record written to storage (status typically <see cref="ArchLucid.Contracts.Common.ArchitectureRunStatus.TasksGenerated"/>).</summary>
    public ArchitectureRun Run { get; set; } = new();
    /// <summary>Evidence bundle referenced by starter agent tasks.</summary>
    public EvidenceBundle EvidenceBundle { get; set; } = new();
    /// <summary>Topology, cost, compliance, and critic starter tasks.</summary>
    public List<AgentTask> Tasks { get; set; } = [];

    /// <summary><see langword="true"/> when this result was produced from a prior <c>Idempotency-Key</c> (HTTP 200 replay).</summary>
    public bool IdempotentReplay
    {
        get; set;
    }
}

/// <summary>
/// Outcome of <see cref="Runs.Orchestration.IArchitectureRunExecuteOrchestrator.ExecuteRunAsync"/>: persisted agent outputs for the run.
/// </summary>
/// <remarks>
///     On failure, <see cref="Runs.Orchestration.IArchitectureRunExecuteOrchestrator.ExecuteRunAsync"/> throws before returning.
///     Baseline mutation audit (<c>Baseline.Architecture.RunFailed</c>) then records exception type, or
///     <c>CircuitBreakerOpenException:CircuitBreakerRejected</c> / <c>LlmTokenQuotaExceededException:LlmTokenQuotaExceeded</c>
///     when the root cause maps to <see cref="AgentExecutionTraceFailureReasonCodes" /> (see execution traces).
/// </remarks>
public sealed class ExecuteRunResult
{
    /// <summary>Same run id passed to execute.</summary>
    public string RunId { get; set; } = string.Empty;
    /// <summary>Agent results stored for this run (possibly loaded idempotently).</summary>
    public List<AgentResult> Results { get; set; } = [];
}

/// <summary>
/// Outcome of <see cref="Runs.Orchestration.IArchitectureRunCommitOrchestrator.CommitRunAsync"/>: committed golden manifest and associated decision traces.
/// </summary>
public sealed class CommitRunResult
{
    /// <summary>Golden manifest produced by merge and persisted for this commit.</summary>
    public GoldenManifest Manifest { get; set; } = new();
    /// <summary>Decision traces persisted with the manifest.</summary>
    public List<DecisionTrace> DecisionTraces { get; set; } = [];
    /// <summary>Non-fatal merge warnings (empty when none).</summary>
    public List<string> Warnings { get; set; } = [];
}
