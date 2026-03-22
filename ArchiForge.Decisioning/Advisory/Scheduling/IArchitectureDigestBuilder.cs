using ArchiForge.Decisioning.Advisory.Models;
using ArchiForge.Decisioning.Alerts;

namespace ArchiForge.Decisioning.Advisory.Scheduling;

public interface IArchitectureDigestBuilder
{
    ArchitectureDigest Build(
        Guid tenantId,
        Guid workspaceId,
        Guid projectId,
        Guid? runId,
        Guid? comparedToRunId,
        ImprovementPlan plan,
        IReadOnlyList<AlertRecord>? evaluatedAlerts = null);
}
