using ArchiForge.Core.Scoping;
using ArchiForge.Decisioning.Compliance.Loaders;
using ArchiForge.Decisioning.Compliance.Models;
using ArchiForge.Decisioning.Governance.PolicyPacks;

namespace ArchiForge.Persistence.Compliance;

/// <summary>
/// Loads the file-based compliance rule pack, then restricts rules to those listed in effective policy content.
/// Scope comes from <see cref="IScopeContextProvider"/> (HTTP or test headers); background jobs should set scope accordingly.
/// </summary>
public sealed class PolicyFilteredComplianceRulePackProvider(
    IComplianceRulePackLoader loader,
    IEffectiveGovernanceLoader governanceLoader,
    IScopeContextProvider scopeProvider) : IComplianceRulePackProvider
{
    public async Task<ComplianceRulePack> GetRulePackAsync(CancellationToken ct)
    {
        var full = await loader.LoadAsync(ct).ConfigureAwait(false);
        var scope = scopeProvider.GetCurrentScope();
        var effective = await governanceLoader
            .LoadEffectiveContentAsync(scope.TenantId, scope.WorkspaceId, scope.ProjectId, ct)
            .ConfigureAwait(false);
        return ComplianceRulePackGovernanceFilter.Filter(full, effective);
    }
}
