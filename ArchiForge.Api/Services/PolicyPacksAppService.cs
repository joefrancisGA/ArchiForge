using System.Text.Json;

using ArchiForge.Core.Audit;
using ArchiForge.Decisioning.Governance.PolicyPacks;

namespace ArchiForge.Api.Services;

/// <summary>
/// Default <see cref="IPolicyPacksAppService"/>: mutates packs through <see cref="IPolicyPackManagementService"/> and writes audit trails.
/// </summary>
/// <remarks>
/// Registered scoped in <c>ServiceCollectionExtensions</c>. Uses <see cref="IPolicyPackVersionRepository"/> only for assign preflight
/// (version existence) before delegating persistence to the management service.
/// </remarks>
/// <param name="managementService">Domain mutations (create / publish / assign row).</param>
/// <param name="versionRepository">Read path for assign 404 semantics.</param>
/// <param name="auditService">Structured audit log.</param>
public sealed class PolicyPacksAppService(
    IPolicyPackManagementService managementService,
    IPolicyPackVersionRepository versionRepository,
    IAuditService auditService) : IPolicyPacksAppService
{
    /// <inheritdoc />
    /// <remarks>Audit payload: pack id, name, pack type (minimal PII).</remarks>
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
        PolicyPack pack = await managementService
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

    /// <inheritdoc />
    /// <remarks>Audit payload: policy pack id and published version label.</remarks>
    public async Task<PolicyPackVersion> PublishVersionAsync(
        Guid policyPackId,
        string version,
        string contentJson,
        CancellationToken ct)
    {
        PolicyPackVersion packVersion = await managementService
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

    /// <inheritdoc />
    /// <remarks>
    /// Early return <c>null</c> avoids creating orphan assignment rows when the client references a non-existent version.
    /// Audit includes assignment id, scope level, and pin flag for SIEM correlation.
    /// </remarks>
    public async Task<PolicyPackAssignment?> TryAssignAsync(
        Guid tenantId,
        Guid workspaceId,
        Guid projectId,
        Guid policyPackId,
        string version,
        string scopeLevel,
        bool isPinned,
        CancellationToken ct)
    {
        PolicyPackVersion? packVersion = await versionRepository
            .GetByPackAndVersionAsync(policyPackId, version, ct)
            .ConfigureAwait(false);
        if (packVersion is null)
            return null;

        PolicyPackAssignment assignment = await managementService
            .AssignAsync(tenantId, workspaceId, projectId, policyPackId, version, scopeLevel, isPinned, ct)
            .ConfigureAwait(false);

        await auditService.LogAsync(
            new AuditEvent
            {
                EventType = AuditEventTypes.PolicyPackAssignmentCreated,
                DataJson = JsonSerializer.Serialize(
                    new
                    {
                        assignment.AssignmentId,
                        policyPackId,
                        version = assignment.PolicyPackVersion,
                        assignment.ScopeLevel,
                        assignment.IsPinned,
                    }),
            },
            ct).ConfigureAwait(false);

        return assignment;
    }
}
