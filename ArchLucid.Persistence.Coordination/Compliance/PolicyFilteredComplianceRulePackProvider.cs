using ArchLucid.Core.Scoping;
using ArchLucid.Decisioning.Compliance.Loaders;
using ArchLucid.Decisioning.Compliance.Models;
using ArchLucid.Decisioning.Governance.PolicyPacks;

namespace ArchLucid.Persistence.Coordination.Compliance;

/// <summary>
/// Loads the file-based compliance rule pack, then restricts rules to those listed in effective policy content.
/// Scope comes from <see cref="IScopeContextProvider"/> (HTTP or test headers); background jobs should set scope accordingly.
/// </summary>
/// <param name="loader">Baseline pack from file/embedded sources (full rule universe).</param>
/// <param name="governanceLoader">Merged effective policy content for the current tenant/workspace/project.</param>
/// <param name="scopeProvider">Ambient scope for the request or background worker.</param>
/// <remarks>
/// Registered as <see cref="IComplianceRulePackProvider"/> in DI. Primary callers: compliance checks that resolve rules per scope.
/// Uses <see cref="ComplianceRulePackGovernanceFilter.Filter"/> so only rules referenced (and enabled) in effective governance remain.
/// </remarks>
public sealed class PolicyFilteredComplianceRulePackProvider(
    IComplianceRulePackLoader loader,
    IEffectiveGovernanceLoader governanceLoader,
    IScopeContextProvider scopeProvider) : IComplianceRulePackProvider
{
    /// <summary>
    /// Loads the full pack, resolves <see cref="IEffectiveGovernanceLoader.LoadEffectiveContentAsync"/> for the ambient scope, then filters rules.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A <see cref="ComplianceRulePack"/> whose rules are intersected with merged <see cref="PolicyPackContentDocument"/>; never <c>null</c>.</returns>
    public async Task<ComplianceRulePack> GetRulePackAsync(CancellationToken ct)
    {
        ComplianceRulePack full = await loader.LoadAsync(ct);
        ScopeContext scope = scopeProvider.GetCurrentScope();
        PolicyPackContentDocument effective = await governanceLoader
            .LoadEffectiveContentAsync(scope.TenantId, scope.WorkspaceId, scope.ProjectId, ct)
            ;
        return ComplianceRulePackGovernanceFilter.Filter(full, effective);
    }
}
