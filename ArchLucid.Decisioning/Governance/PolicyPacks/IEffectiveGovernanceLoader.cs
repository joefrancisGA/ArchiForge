using ArchLucid.Decisioning.Governance.Resolution;

namespace ArchLucid.Decisioning.Governance.PolicyPacks;

/// <summary>
///     Thin façade that exposes only the merged <see cref="PolicyPackContentDocument" /> for a scope—hiding resolution
///     decisions/conflicts from consumers that only need the effective payload (alerts, compliance, advisory).
/// </summary>
/// <remarks>
///     <para>
///         <strong>Why:</strong> Call sites such as <c>AlertService</c>, <c>CompositeAlertService</c>,
///         <c>AdvisoryScanRunner</c>,
///         and <c>PolicyFilteredComplianceRulePackProvider</c> should not duplicate merge logic; they load effective
///         content once per operation.
///     </para>
///     <para>
///         Implementation delegates to <see cref="IEffectiveGovernanceResolver.ResolveAsync" /> and returns
///         <see cref="EffectiveGovernanceResolutionResult.EffectiveContent" /> only. For full traceability use the
///         resolver or
///         HTTP <c>GET /v1/governance-resolution</c>.
///     </para>
/// </remarks>
public interface IEffectiveGovernanceLoader
{
    /// <summary>Returns the resolved effective governance document for the given scope (no per-item provenance).</summary>
    /// <param name="tenantId">Tenant id for the scope.</param>
    /// <param name="workspaceId">Workspace id for the scope.</param>
    /// <param name="projectId">Project id for the scope.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    ///     Merged <see cref="PolicyPackContentDocument" /> after hierarchical resolution; may be empty when no assignments
    ///     apply.
    /// </returns>
    Task<PolicyPackContentDocument> LoadEffectiveContentAsync(
        Guid tenantId,
        Guid workspaceId,
        Guid projectId,
        CancellationToken ct);
}
