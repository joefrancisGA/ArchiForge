using ArchiForge.Decisioning.Governance.Resolution;

namespace ArchiForge.Decisioning.Governance.PolicyPacks;

/// <summary>
/// Domain service for <strong>mutating</strong> policy packs: create pack, publish version, assign version to a scope tier.
/// Does not perform HTTP validation or audit logging (see <c>ArchiForge.Api.Services.PolicyPacksAppService</c>).
/// </summary>
/// <remarks>
/// Registered scoped in the API. Persistence is abstracted via <see cref="IPolicyPackRepository"/>,
/// <see cref="IPolicyPackVersionRepository"/>, and <see cref="IPolicyPackAssignmentRepository"/>.
/// </remarks>
public interface IPolicyPackManagementService
{
    /// <summary>Creates a new pack scoped to tenant/workspace/project and seeds an initial <strong>unpublished</strong> version <c>1.0.0</c>.</summary>
    /// <param name="tenantId">Owning tenant.</param>
    /// <param name="workspaceId">Owning workspace (pack metadata scope).</param>
    /// <param name="projectId">Owning project (pack metadata scope).</param>
    /// <param name="name">Display name.</param>
    /// <param name="description">Optional description.</param>
    /// <param name="packType">One of <see cref="PolicyPackType"/> constants.</param>
    /// <param name="initialContentJson">JSON body for the first version; normalized to <c>{}</c> if blank.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The persisted <see cref="PolicyPack"/> in <see cref="PolicyPackStatus.Draft"/>.</returns>
    /// <remarks>
    /// Caller is typically <c>PolicyPacksAppService.CreatePackAsync</c> after API validation. Also used from integration tests.
    /// </remarks>
    Task<PolicyPack> CreatePackAsync(
        Guid tenantId,
        Guid workspaceId,
        Guid projectId,
        string name,
        string description,
        string packType,
        string initialContentJson,
        CancellationToken ct);

    /// <summary>
    /// Publishes (or re-publishes) a semantic version for a pack: upserts <see cref="PolicyPackVersion"/>, marks published,
    /// and updates the parent pack to <see cref="PolicyPackStatus.Active"/> with <see cref="PolicyPack.CurrentVersion"/> set.
    /// </summary>
    /// <param name="policyPackId">Target pack id.</param>
    /// <param name="version">SemVer label (validated at API layer).</param>
    /// <param name="contentJson">Full <see cref="PolicyPackContentDocument"/> JSON; normalized to <c>{}</c> if blank.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The version row that was created or updated.</returns>
    /// <remarks>
    /// Re-publishing the same <paramref name="version"/> updates <c>ContentJson</c> in place (idempotent version row).
    /// Called from <c>PolicyPacksAppService.PublishVersionAsync</c>.
    /// </remarks>
    Task<PolicyPackVersion> PublishVersionAsync(
        Guid policyPackId,
        string version,
        string contentJson,
        CancellationToken ct);

    /// <summary>
    /// Persists a new <see cref="PolicyPackAssignment"/> linking a pack version to a governance tier (tenant / workspace / project).
    /// </summary>
    /// <param name="tenantId">Tenant for the assignment (always required).</param>
    /// <param name="workspaceId">Workspace for workspace/project scope; may be stored as <see cref="Guid.Empty"/> for tenant scope.</param>
    /// <param name="projectId">Project for project scope; may be stored as empty for tenant/workspace scope.</param>
    /// <param name="policyPackId">Pack to assign.</param>
    /// <param name="version">Version string that must exist for the pack (caller often pre-checks).</param>
    /// <param name="scopeLevel">Normalized to <see cref="GovernanceScopeLevel"/>; invalid values should be rejected by API validation.</param>
    /// <param name="isPinned">When true, increases precedence within the same scope tier during resolution.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The new assignment row (always enabled).</returns>
    /// <remarks>
    /// Normalizes <paramref name="scopeLevel"/> and clears unused workspace/project ids for tenant/workspace rows so persistence matches
    /// hierarchical matching rules. Called from <c>PolicyPacksAppService.TryAssignAsync</c> after version existence check.
    /// </remarks>
    Task<PolicyPackAssignment> AssignAsync(
        Guid tenantId,
        Guid workspaceId,
        Guid projectId,
        Guid policyPackId,
        string version,
        string scopeLevel,
        bool isPinned,
        CancellationToken ct);

    /// <summary>Soft-deletes an assignment row for the tenant (sets <see cref="PolicyPackAssignment.ArchivedUtc"/>).</summary>
    Task<bool> TryArchiveAssignmentAsync(Guid tenantId, Guid assignmentId, CancellationToken ct);
}
