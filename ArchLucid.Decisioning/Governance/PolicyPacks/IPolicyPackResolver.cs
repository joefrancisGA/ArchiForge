namespace ArchiForge.Decisioning.Governance.PolicyPacks;

/// <summary>
/// Resolves <strong>enabled</strong> policy pack assignments into a flat list of packs with raw <c>ContentJson</c>
/// (no merge / precedence beyond assignment ordering).
/// </summary>
/// <remarks>
/// <para>
/// <strong>Why:</strong> Operators and APIs need to see <em>which</em> packs apply to a scope as separate entries
/// (for debugging and <c>GET …/policy-packs/effective</c>), distinct from <see cref="IEffectiveGovernanceLoader"/> which returns a single merged document.
/// </para>
/// <para>
/// Primary caller: <c>ArchiForge.Api.Controllers.PolicyPacksController.GetEffective</c> and <see cref="PolicyPackResolver"/> itself via DI.
/// </para>
/// </remarks>
public interface IPolicyPackResolver
{
    /// <summary>
    /// Returns all enabled assignments applicable to the scope, each expanded to metadata + published version content.
    /// </summary>
    /// <param name="tenantId">Tenant scope.</param>
    /// <param name="workspaceId">Workspace scope.</param>
    /// <param name="projectId">Project scope.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// <see cref="EffectivePolicyPackSet"/> with zero or more <see cref="ResolvedPolicyPack"/> entries; skipped rows when pack or version missing.
    /// </returns>
    /// <remarks>
    /// Does <strong>not</strong> apply hierarchical merge rules—only repository filtering and enabled flag. Merge semantics live in
    /// <see cref="Resolution.IEffectiveGovernanceResolver"/>.
    /// </remarks>
    Task<EffectivePolicyPackSet> ResolveAsync(
        Guid tenantId,
        Guid workspaceId,
        Guid projectId,
        CancellationToken ct);
}
