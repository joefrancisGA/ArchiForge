using System.Text.Json;
using ArchiForge.Core.Audit;
using ArchiForge.Decisioning.Governance.PolicyPacks;

namespace ArchiForge.Api.Services;

public sealed class PolicyPacksAppService(
    IPolicyPackManagementService managementService,
    IPolicyPackVersionRepository versionRepository,
    IAuditService auditService) : IPolicyPacksAppService
{
    public async Task<PolicyPack> CreatePackAsync(
        Guid tenantId,
        Guid workspaceId,
        Guid projectId,
        string name,
        string description,
        string packType,
        string initialContentJson,
        CancellationToken ct)
    {
        var pack = await managementService
            .CreatePackAsync(tenantId, workspaceId, projectId, name, description, packType, initialContentJson, ct)
            .ConfigureAwait(false);

        await auditService.LogAsync(
            new AuditEvent
            {
                EventType = AuditEventTypes.PolicyPackCreated,
                DataJson = JsonSerializer.Serialize(new { pack.PolicyPackId, pack.Name, pack.PackType }),
            },
            ct).ConfigureAwait(false);

        return pack;
    }

    public async Task<PolicyPackVersion> PublishVersionAsync(
        Guid policyPackId,
        string version,
        string contentJson,
        CancellationToken ct)
    {
        var packVersion = await managementService
            .PublishVersionAsync(policyPackId, version, contentJson, ct)
            .ConfigureAwait(false);

        await auditService.LogAsync(
            new AuditEvent
            {
                EventType = AuditEventTypes.PolicyPackVersionPublished,
                DataJson = JsonSerializer.Serialize(new { policyPackId, packVersion.Version }),
            },
            ct).ConfigureAwait(false);

        return packVersion;
    }

    public async Task<PolicyPackAssignment?> TryAssignAsync(
        Guid tenantId,
        Guid workspaceId,
        Guid projectId,
        Guid policyPackId,
        string version,
        CancellationToken ct)
    {
        var packVersion = await versionRepository
            .GetByPackAndVersionAsync(policyPackId, version, ct)
            .ConfigureAwait(false);
        if (packVersion is null)
            return null;

        var assignment = await managementService
            .AssignAsync(tenantId, workspaceId, projectId, policyPackId, version, ct)
            .ConfigureAwait(false);

        await auditService.LogAsync(
            new AuditEvent
            {
                EventType = AuditEventTypes.PolicyPackAssigned,
                DataJson = JsonSerializer.Serialize(
                    new { assignment.AssignmentId, policyPackId, version = assignment.PolicyPackVersion }),
            },
            ct).ConfigureAwait(false);

        return assignment;
    }
}
