using ArchiForge.Decisioning.Alerts;

namespace ArchiForge.Decisioning.Alerts.Simulation;

public interface IAlertSimulationContextProvider
{
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
