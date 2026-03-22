namespace ArchiForge.Decisioning.Alerts.Simulation;

public interface IRuleSimulationService
{
    Task<RuleSimulationResult> SimulateAsync(
        Guid tenantId,
        Guid workspaceId,
        Guid projectId,
        RuleSimulationRequest request,
        CancellationToken ct);

    Task<RuleCandidateComparisonResult> CompareCandidatesAsync(
        Guid tenantId,
        Guid workspaceId,
        Guid projectId,
        RuleCandidateComparisonRequest request,
        CancellationToken ct);
}
