namespace ArchLucid.Application.Runs.Orchestration;

/// <summary>
///     Executes agent tasks for a run and persists evidence, results, and evaluations (execute phase).
/// </summary>
public interface IArchitectureRunExecuteOrchestrator
{
    Task<ExecuteRunResult> ExecuteRunAsync(string runId, CancellationToken cancellationToken = default);
}
