using ArchLucid.Decisioning.Governance.PolicyPacks;

namespace ArchLucid.Decisioning.Governance.Resolution;

/// <summary>
///     Computes <strong>effective governance</strong> for a tenant/workspace/project: layered policy pack assignments,
///     deterministic precedence, merged <see cref="PolicyPackContentDocument" />, and explainability artifacts
///     (<see cref="GovernanceResolutionDecision" />, <see cref="GovernanceConflictRecord" />).
/// </summary>
/// <remarks>
///     <para>
///         <strong>Why:</strong> Operators need a single resolved document for alerts, compliance, and advisory behavior,
///         plus an audit-friendly trace of <em>which</em> pack won for each rule ID / key / metadata entry and
///         <em>why</em>.
///     </para>
///     <para>
///         <strong>Primary callers:</strong> <see cref="EffectiveGovernanceLoader" /> (effective content for API and
///         runtime),
///         HTTP <c>GET /v1/governance-resolution</c> (implemented in <c>ArchLucid.Api</c>),
///         and <c>ArchLucid.Decisioning.Tests.EffectiveGovernanceResolverTests</c>.
///     </para>
///     <para>
///         Registered scoped in <c>ArchLucid.Api.Startup.ServiceCollectionExtensions</c>.
///     </para>
/// </remarks>
public interface IEffectiveGovernanceResolver
{
    /// <summary>
    ///     Loads all applicable assignments for the scope, resolves pack versions to content, merges by precedence,
    ///     and returns effective content plus decisions and conflicts.
    /// </summary>
    /// <param name="tenantId">Tenant dimension of the scope (from ambient scope / HTTP headers).</param>
    /// <param name="workspaceId">Workspace dimension; must match workspace- and project-scoped assignments.</param>
    /// <param name="projectId">Project dimension; must match project-scoped assignments.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    ///     A populated <see cref="EffectiveGovernanceResolutionResult" />;
    ///     <see cref="EffectiveGovernanceResolutionResult.EffectiveContent" />
    ///     may be empty if no packs apply or all content JSON is invalid.
    ///     <see cref="EffectiveGovernanceResolutionResult.Notes" /> always include summary counts.
    /// </returns>
    /// <remarks>
    ///     Delegates to <see cref="IPolicyPackAssignmentRepository.ListByScopeAsync" /> (repository applies hierarchical scope
    ///     matching),
    ///     then applies <see cref="EffectiveGovernanceResolver" /> filtering and merge rules. Malformed <c>ContentJson</c> or
    ///     missing
    ///     pack/version rows are skipped so one bad assignment does not fail the entire resolution.
    /// </remarks>
    Task<EffectiveGovernanceResolutionResult> ResolveAsync(
        Guid tenantId,
        Guid workspaceId,
        Guid projectId,
        CancellationToken ct);
}
