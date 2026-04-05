using ArchiForge.Decisioning.Governance.PolicyPacks;
using ArchiForge.Decisioning.Governance.Resolution;

using FsCheck.Xunit;

namespace ArchiForge.Decisioning.Tests;

/// <summary>
/// Property checks for precedence ordering used during effective governance merge.
/// </summary>
[Trait("Suite", "Core")]
public sealed class EffectiveGovernanceResolverPropertyTests
{
    [Property(MaxTest = 100)]
    public void Project_scope_always_outranks_tenant_scope_regardless_of_pin(bool tenantPinned, bool projectPinned)
    {
        PolicyPackAssignment tenant = new()
        {
            ScopeLevel = GovernanceScopeLevel.Tenant,
            IsPinned = tenantPinned,
        };

        PolicyPackAssignment project = new()
        {
            ScopeLevel = GovernanceScopeLevel.Project,
            IsPinned = projectPinned,
        };

        int tenantRank = EffectiveGovernanceResolver.GetPrecedenceRank(tenant);
        int projectRank = EffectiveGovernanceResolver.GetPrecedenceRank(project);

        Assert.True(projectRank > tenantRank);
    }

    [Property(MaxTest = 100)]
    public void Workspace_scope_outranks_tenant_scope(bool tenantPinned, bool workspacePinned)
    {
        PolicyPackAssignment tenant = new()
        {
            ScopeLevel = GovernanceScopeLevel.Tenant,
            IsPinned = tenantPinned,
        };

        PolicyPackAssignment workspace = new()
        {
            ScopeLevel = GovernanceScopeLevel.Workspace,
            IsPinned = workspacePinned,
        };

        Assert.True(
            EffectiveGovernanceResolver.GetPrecedenceRank(workspace) >
            EffectiveGovernanceResolver.GetPrecedenceRank(tenant));
    }

    [Property(MaxTest = 100)]
    public void Pinned_assignment_gains_100_over_unpinned_at_same_scope(bool pinned)
    {
        PolicyPackAssignment a = new()
        {
            ScopeLevel = GovernanceScopeLevel.Workspace,
            IsPinned = pinned,
        };

        PolicyPackAssignment b = new()
        {
            ScopeLevel = GovernanceScopeLevel.Workspace,
            IsPinned = !pinned,
        };

        if (pinned)
            Assert.Equal(100, EffectiveGovernanceResolver.GetPrecedenceRank(a) - EffectiveGovernanceResolver.GetPrecedenceRank(b));
        else
            Assert.Equal(-100, EffectiveGovernanceResolver.GetPrecedenceRank(a) - EffectiveGovernanceResolver.GetPrecedenceRank(b));
    }
}
