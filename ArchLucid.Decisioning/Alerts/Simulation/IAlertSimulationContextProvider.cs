namespace ArchLucid.Decisioning.Alerts.Simulation;

/// <summary>
///     Builds one or more <see cref="AlertEvaluationContext" /> instances for “what-if” alert evaluation without
///     persisting a scan.
/// </summary>
/// <remarks>
///     Implemented by <c>ArchLucid.Persistence.Alerts.Simulation.AlertSimulationContextProvider</c>. Contexts omit
///     <see cref="AlertEvaluationContext.EffectiveGovernanceContent" /> unless extended later; evaluators then load
///     governance via the alert services.
/// </remarks>
public interface IAlertSimulationContextProvider
{
    /// <summary>
    ///     When <paramref name="runId" /> is set, builds a single context for that run (optional comparison). Otherwise loads
    ///     recent runs for the slug and builds one context per run.
    /// </summary>
    /// <param name="tenantId">Scope tenant.</param>
    /// <param name="workspaceId">Scope workspace.</param>
    /// <param name="projectId">Scope project.</param>
    /// <param name="runId">Specific run, or <c>null</c> to enumerate recent runs.</param>
    /// <param name="comparedToRunId">Baseline run for comparison when <paramref name="runId" /> is set.</param>
    /// <param name="recentRunCount">When enumerating runs, clamped to a safe maximum (e.g. 50).</param>
    /// <param name="runProjectSlug">Project slug passed to authority query (e.g. <c>default</c>).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Zero or more contexts; skips runs without a golden manifest.</returns>
    Task<IReadOnlyList<AlertEvaluationContext>> GetContextsAsync(
        Guid tenantId,
        Guid workspaceId,
        Guid projectId,
        Guid? runId,
        Guid? comparedToRunId,
        int recentRunCount,
        string runProjectSlug,
        CancellationToken ct);
}
