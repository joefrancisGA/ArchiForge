namespace ArchiForge.Decisioning.Governance.PolicyPacks;

/// <summary>
/// Persistence port for <see cref="PolicyPackAssignment"/> rows: create, toggle/update, and list by hierarchical scope.
/// </summary>
/// <remarks>
/// Implemented by <c>ArchiForge.Persistence.Governance.DapperPolicyPackAssignmentRepository</c> (SQL Server) and
/// <c>InMemoryPolicyPackAssignmentRepository</c> (tests / in-memory storage). List semantics must align with
/// <see cref="Resolution.IEffectiveGovernanceResolver"/> and <see cref="PolicyPackResolver"/> expectations.
/// </remarks>
public interface IPolicyPackAssignmentRepository
{
    /// <summary>Inserts a new assignment row.</summary>
    /// <param name="assignment">Entity to persist (typically built by <see cref="IPolicyPackManagementService.AssignAsync"/>).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <remarks>Called from management service after API assign; also from tests.</remarks>
    Task CreateAsync(PolicyPackAssignment assignment, CancellationToken ct);

    /// <summary>Updates mutable columns (e.g. <see cref="PolicyPackAssignment.IsEnabled"/>).</summary>
    /// <param name="assignment">Row with updated fields.</param>
    /// <param name="ct">Cancellation token.</param>
    Task UpdateAsync(PolicyPackAssignment assignment, CancellationToken ct);

    /// <summary>
    /// Returns assignments whose <see cref="PolicyPackAssignment.ScopeLevel"/> and ids match the given project context
    /// (tenant-wide, workspace-wide, and project-specific rows).
    /// </summary>
    /// <param name="tenantId">Current tenant.</param>
    /// <param name="workspaceId">Current workspace.</param>
    /// <param name="projectId">Current project.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Matching rows, typically ordered by <see cref="PolicyPackAssignment.AssignedUtc"/> descending; may include disabled rows.</returns>
    /// <remarks>
    /// Consumers filter <see cref="PolicyPackAssignment.IsEnabled"/> themselves. Critical call paths:
    /// <see cref="PolicyPackResolver.ResolveAsync"/> and <see cref="Resolution.EffectiveGovernanceResolver.ResolveAsync"/>.
    /// </remarks>
    Task<IReadOnlyList<PolicyPackAssignment>> ListByScopeAsync(
        Guid tenantId,
        Guid workspaceId,
        Guid projectId,
        CancellationToken ct);

    /// <summary>Sets <see cref="PolicyPackAssignment.ArchivedUtc"/> when the row belongs to <paramref name="tenantId"/> and is not already archived.</summary>
    /// <returns>True when a row was updated.</returns>
    Task<bool> ArchiveAsync(Guid tenantId, Guid assignmentId, CancellationToken ct);
}
