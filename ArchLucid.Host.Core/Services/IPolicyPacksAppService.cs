using ArchiForge.Decisioning.Governance.PolicyPacks;

namespace ArchiForge.Host.Core.Services;

/// <summary>
/// Application-layer orchestration for policy pack mutations: delegates to <see cref="IPolicyPackManagementService"/> and emits audit events.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Why:</strong> Keeps the policy packs HTTP controller thin (validation only) and centralizes audit payload shape for create / publish / assign.
/// </para>
/// <para>
/// <strong>Callers:</strong> Policy packs controller in the API host; tests may substitute fakes.
/// </para>
/// </remarks>
public interface IPolicyPacksAppService
{
    /// <summary>Creates a pack and initial draft version; logs <c>PolicyPackCreated</c>.</summary>
    /// <param name="tenantId">From <c>IScopeContextProvider</c>.</param>
    /// <param name="workspaceId">From scope.</param>
    /// <param name="projectId">From scope.</param>
    /// <param name="name">Validated request name.</param>
    /// <param name="description">Optional description.</param>
    /// <param name="packType">Validated pack type string.</param>
    /// <param name="initialContentJson">JSON for version 1.0.0 draft.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Created <see cref="PolicyPack"/>.</returns>
    Task<PolicyPack> CreatePackAsync(
        Guid tenantId,
        Guid workspaceId,
        Guid projectId,
        string name,
        string description,
        string packType,
        string initialContentJson,
        CancellationToken ct);

    /// <summary>Publishes or updates a version row; logs <c>PolicyPackVersionPublished</c>.</summary>
    /// <param name="policyPackId">Pack id from route.</param>
    /// <param name="version">Trimmed SemVer from request.</param>
    /// <param name="contentJson">Full content JSON.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<PolicyPackVersion> PublishVersionAsync(
        Guid policyPackId,
        string version,
        string contentJson,
        CancellationToken ct);

    /// <summary>
    /// Assigns a version to a governance tier when the version row exists; logs <c>PolicyPackAssignmentCreated</c>.
    /// </summary>
    /// <returns><c>null</c> when <see cref="IPolicyPackVersionRepository.GetByPackAndVersionAsync"/> finds no row (caller returns HTTP 404).</returns>
    /// <remarks>Precondition: API FluentValidation has already validated <paramref name="scopeLevel"/> and SemVer <paramref name="version"/>.</remarks>
    Task<PolicyPackAssignment?> TryAssignAsync(
        Guid tenantId,
        Guid workspaceId,
        Guid projectId,
        Guid policyPackId,
        string version,
        string scopeLevel,
        bool isPinned,
        CancellationToken ct);

    /// <summary>Archives an assignment in the current tenant; logs <c>PolicyPackAssignmentArchived</c> when successful.</summary>
    Task<bool> TryArchiveAssignmentAsync(Guid tenantId, Guid assignmentId, CancellationToken ct);
}
