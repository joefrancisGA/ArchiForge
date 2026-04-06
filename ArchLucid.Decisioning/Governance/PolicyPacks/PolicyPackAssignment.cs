using ArchiForge.Decisioning.Governance.Resolution;

namespace ArchiForge.Decisioning.Governance.PolicyPacks;

/// <summary>
/// Binds a published <see cref="PolicyPackVersion"/> to a governance tier (tenant, workspace, or project) for a tenant.
/// </summary>
/// <remarks>
/// Rows are queried hierarchically (see <see cref="IPolicyPackAssignmentRepository.ListByScopeAsync"/>). Unused scope dimensions are stored as
/// <see cref="Guid.Empty"/> for tenant/workspace-only assignments. Consumed by <see cref="PolicyPackResolver"/> and
/// <see cref="IEffectiveGovernanceResolver"/>.
/// </remarks>
public class PolicyPackAssignment
{
    /// <summary>Primary key for the assignment row.</summary>
    public Guid AssignmentId { get; set; } = Guid.NewGuid();

    /// <summary>Tenant that owns this assignment (always required).</summary>
    public Guid TenantId { get; set; }

    /// <summary>Workspace dimension; empty when <see cref="ScopeLevel"/> is <see cref="GovernanceScopeLevel.Tenant"/>.</summary>
    public Guid WorkspaceId { get; set; }

    /// <summary>Project dimension; empty for tenant or workspace scope levels.</summary>
    public Guid ProjectId { get; set; }

    /// <summary>Pack being assigned.</summary>
    public Guid PolicyPackId { get; set; }

    /// <summary>Version label referencing <see cref="PolicyPackVersion.Version"/>.</summary>
    public string PolicyPackVersion { get; set; } = null!;

    /// <summary>When false, the assignment is ignored by resolvers (soft off).</summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>Tenant, Workspace, or Project — see <see cref="GovernanceScopeLevel"/>.</summary>
    public string ScopeLevel { get; set; } = GovernanceScopeLevel.Project;

    /// <summary>Raises precedence within the same <see cref="ScopeLevel"/> during merge.</summary>
    public bool IsPinned { get; set; }

    /// <summary>Creation / last-assign timestamp (tie-breaker after rank).</summary>
    public DateTime AssignedUtc { get; set; } = DateTime.UtcNow;

    /// <summary>When set, the assignment is retained for audit but ignored by <see cref="IPolicyPackAssignmentRepository.ListByScopeAsync"/>.</summary>
    public DateTime? ArchivedUtc { get; set; }
}
