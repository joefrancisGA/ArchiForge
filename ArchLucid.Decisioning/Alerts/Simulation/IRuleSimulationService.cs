namespace ArchLucid.Decisioning.Alerts.Simulation;

/// <summary>
///     What-if evaluation of simple or composite rules over historical run contexts (no alert persistence from simple
///     rules; composite uses live suppression reads).
/// </summary>
/// <remarks>
///     Implemented by <c>ArchLucid.Persistence.Alerts.Simulation.RuleSimulationService</c>. Exposed via
///     <c>AlertSimulationController</c>.
/// </remarks>
public interface IRuleSimulationService
{
    /// <summary>
    ///     Builds contexts via <see cref="IAlertSimulationContextProvider" />, evaluates the requested rule kind per context,
    ///     and aggregates counts and outcomes.
    /// </summary>
    Task<RuleSimulationResult> SimulateAsync(
        Guid tenantId,
        Guid workspaceId,
        Guid projectId,
        RuleSimulationRequest request,
        CancellationToken ct);

    /// <summary>
    ///     Runs <see cref="SimulateAsync" /> twice (candidate A vs B) with shared window settings and returns side-by-side
    ///     results plus summary notes.
    /// </summary>
    Task<RuleCandidateComparisonResult> CompareCandidatesAsync(
        Guid tenantId,
        Guid workspaceId,
        Guid projectId,
        RuleCandidateComparisonRequest request,
        CancellationToken ct);
}
