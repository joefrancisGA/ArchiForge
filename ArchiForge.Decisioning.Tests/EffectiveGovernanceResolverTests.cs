using ArchiForge.Decisioning.Governance.PolicyPacks;
using ArchiForge.Decisioning.Governance.Resolution;
using ArchiForge.Persistence.Governance;

using FluentAssertions;

namespace ArchiForge.Decisioning.Tests;

[Trait("Suite", "Core")]
public sealed class EffectiveGovernanceResolverTests
{
    [Fact]
    public async Task Project_scope_wins_over_tenant_for_same_metadata_key_and_records_value_conflict()
    {
        Guid tenantId = Guid.NewGuid();
        Guid workspaceId = Guid.NewGuid();
        Guid projectId = Guid.NewGuid();

        InMemoryPolicyPackRepository packRepo = new();
        InMemoryPolicyPackVersionRepository versionRepo = new();
        InMemoryPolicyPackAssignmentRepository assignmentRepo = new();

        Guid tenantPackId = Guid.NewGuid();
        Guid projectPackId = Guid.NewGuid();

        await packRepo.CreateAsync(
            new PolicyPack
            {
                PolicyPackId = tenantPackId,
                TenantId = tenantId,
                WorkspaceId = workspaceId,
                ProjectId = projectId,
                Name = "Tenant baseline",
                Description = "",
                PackType = PolicyPackType.TenantCustom,
                Status = PolicyPackStatus.Active,
                CurrentVersion = "1.0.0",
            },
            CancellationToken.None);

        await packRepo.CreateAsync(
            new PolicyPack
            {
                PolicyPackId = projectPackId,
                TenantId = tenantId,
                WorkspaceId = workspaceId,
                ProjectId = projectId,
                Name = "Project override",
                Description = "",
                PackType = PolicyPackType.ProjectCustom,
                Status = PolicyPackStatus.Active,
                CurrentVersion = "1.0.0",
            },
            CancellationToken.None);

        await versionRepo.CreateAsync(
            new PolicyPackVersion
            {
                PolicyPackVersionId = Guid.NewGuid(),
                PolicyPackId = tenantPackId,
                Version = "1.0.0",
                ContentJson = """{"metadata":{"tier":"tenant"},"complianceRuleIds":[],"complianceRuleKeys":[],"alertRuleIds":[],"compositeAlertRuleIds":[],"advisoryDefaults":{}}""",
                CreatedUtc = DateTime.UtcNow,
                IsPublished = true,
            },
            CancellationToken.None);

        await versionRepo.CreateAsync(
            new PolicyPackVersion
            {
                PolicyPackVersionId = Guid.NewGuid(),
                PolicyPackId = projectPackId,
                Version = "1.0.0",
                ContentJson = """{"metadata":{"tier":"project"},"complianceRuleIds":[],"complianceRuleKeys":[],"alertRuleIds":[],"compositeAlertRuleIds":[],"advisoryDefaults":{}}""",
                CreatedUtc = DateTime.UtcNow,
                IsPublished = true,
            },
            CancellationToken.None);

        await assignmentRepo.CreateAsync(
            new PolicyPackAssignment
            {
                TenantId = tenantId,
                WorkspaceId = Guid.Empty,
                ProjectId = Guid.Empty,
                PolicyPackId = tenantPackId,
                PolicyPackVersion = "1.0.0",
                ScopeLevel = GovernanceScopeLevel.Tenant,
                AssignedUtc = DateTime.UtcNow.AddMinutes(-1),
            },
            CancellationToken.None);

        await assignmentRepo.CreateAsync(
            new PolicyPackAssignment
            {
                TenantId = tenantId,
                WorkspaceId = workspaceId,
                ProjectId = projectId,
                PolicyPackId = projectPackId,
                PolicyPackVersion = "1.0.0",
                ScopeLevel = GovernanceScopeLevel.Project,
                AssignedUtc = DateTime.UtcNow,
            },
            CancellationToken.None);

        EffectiveGovernanceResolver resolver = new(assignmentRepo, packRepo, versionRepo);

        EffectiveGovernanceResolutionResult result = await resolver.ResolveAsync(tenantId, workspaceId, projectId, CancellationToken.None);

        result.EffectiveContent.Metadata["tier"].Should().Be("project");
        result.Conflicts.Should().ContainSingle(c => c.ConflictType == "ValueConflict" && c.ItemKey == "tier");
        result.Decisions.Should().Contain(d => d.ItemType == "Metadata" && d.ItemKey == "tier" && d.WinningPolicyPackId == projectPackId);
    }
}
