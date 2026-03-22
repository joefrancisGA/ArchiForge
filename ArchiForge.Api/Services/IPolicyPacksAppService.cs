using ArchiForge.Decisioning.Governance.PolicyPacks;

namespace ArchiForge.Api.Services;

/// <summary>Orchestrates policy pack mutations with audit logging (keeps controllers thin).</summary>
public interface IPolicyPacksAppService
{
    Task<PolicyPack> CreatePackAsync(
        Guid tenantId,
        Guid workspaceId,
        Guid projectId,
        string name,
        string description,
        string packType,
        string initialContentJson,
        CancellationToken ct);

    Task<PolicyPackVersion> PublishVersionAsync(
        Guid policyPackId,
        string version,
        string contentJson,
        CancellationToken ct);

    /// <summary>Returns null when no version row exists for the pack (caller maps to 404).</summary>
    Task<PolicyPackAssignment?> TryAssignAsync(
        Guid tenantId,
        Guid workspaceId,
        Guid projectId,
        Guid policyPackId,
        string version,
        CancellationToken ct);
}
