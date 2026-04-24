using ArchLucid.Decisioning.Governance.PolicyPacks;
using ArchLucid.Decisioning.Governance.Resolution;
using ArchLucid.Persistence.Governance;

using FluentAssertions;

namespace ArchLucid.Decisioning.Tests;

/// <summary>
/// Tests for Effective Governance Resolver.
/// </summary>

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

    [Fact]
    public async Task Archived_assignments_are_not_visible_to_resolver_lists()
    {
        Guid tenantId = Guid.NewGuid();
        Guid workspaceId = Guid.NewGuid();
        Guid projectId = Guid.NewGuid();
        Guid packId = Guid.NewGuid();

        InMemoryPolicyPackRepository packRepo = new();
        InMemoryPolicyPackVersionRepository versionRepo = new();
        InMemoryPolicyPackAssignmentRepository assignmentRepo = new();

        await packRepo.CreateAsync(
            new PolicyPack
            {
                PolicyPackId = packId,
                TenantId = tenantId,
                WorkspaceId = workspaceId,
                ProjectId = projectId,
                Name = "Only archived assign",
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
                PolicyPackId = packId,
                Version = "1.0.0",
                ContentJson = """{"metadata":{"orphan":"x"},"complianceRuleIds":[],"complianceRuleKeys":[],"alertRuleIds":[],"compositeAlertRuleIds":[],"advisoryDefaults":{}}""",
                CreatedUtc = DateTime.UtcNow,
                IsPublished = true,
            },
            CancellationToken.None);

        await assignmentRepo.CreateAsync(
            new PolicyPackAssignment
            {
                TenantId = tenantId,
                WorkspaceId = workspaceId,
                ProjectId = projectId,
                PolicyPackId = packId,
                PolicyPackVersion = "1.0.0",
                ScopeLevel = GovernanceScopeLevel.Project,
                AssignedUtc = DateTime.UtcNow,
                ArchivedUtc = DateTime.UtcNow,
            },
            CancellationToken.None);

        EffectiveGovernanceResolver resolver = new(assignmentRepo, packRepo, versionRepo);

        EffectiveGovernanceResolutionResult result = await resolver.ResolveAsync(tenantId, workspaceId, projectId, CancellationToken.None);

        result.EffectiveContent.Metadata.Should().NotContainKey("orphan");
    }

    [Fact]
    public async Task ResolveAsync_Skips_assignment_when_policy_pack_row_missing()
    {
        Guid tenantId = Guid.NewGuid();
        Guid workspaceId = Guid.NewGuid();
        Guid projectId = Guid.NewGuid();
        Guid missingPackId = Guid.NewGuid();

        InMemoryPolicyPackRepository packRepo = new();
        InMemoryPolicyPackVersionRepository versionRepo = new();
        InMemoryPolicyPackAssignmentRepository assignmentRepo = new();

        await assignmentRepo.CreateAsync(
            new PolicyPackAssignment
            {
                TenantId = tenantId,
                WorkspaceId = workspaceId,
                ProjectId = projectId,
                PolicyPackId = missingPackId,
                PolicyPackVersion = "1.0.0",
                ScopeLevel = GovernanceScopeLevel.Project,
                AssignedUtc = DateTime.UtcNow,
            },
            CancellationToken.None);

        EffectiveGovernanceResolver resolver = new(assignmentRepo, packRepo, versionRepo);
        EffectiveGovernanceResolutionResult result = await resolver.ResolveAsync(tenantId, workspaceId, projectId, CancellationToken.None);

        result.Notes.Should()
            .Contain(n => n.Contains("pack not found", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task ResolveAsync_Skips_assignment_when_assigned_version_row_missing()
    {
        Guid tenantId = Guid.NewGuid();
        Guid workspaceId = Guid.NewGuid();
        Guid projectId = Guid.NewGuid();
        Guid packId = Guid.NewGuid();

        InMemoryPolicyPackRepository packRepo = new();
        InMemoryPolicyPackVersionRepository versionRepo = new();
        InMemoryPolicyPackAssignmentRepository assignmentRepo = new();

        await packRepo.CreateAsync(
            new PolicyPack
            {
                PolicyPackId = packId,
                TenantId = tenantId,
                WorkspaceId = workspaceId,
                ProjectId = projectId,
                Name = "Has pack",
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
                PolicyPackId = packId,
                Version = "1.0.0",
                ContentJson = """{"metadata":{},"complianceRuleIds":[],"complianceRuleKeys":[],"alertRuleIds":[],"compositeAlertRuleIds":[],"advisoryDefaults":{}}""",
                CreatedUtc = DateTime.UtcNow,
                IsPublished = true,
            },
            CancellationToken.None);

        await assignmentRepo.CreateAsync(
            new PolicyPackAssignment
            {
                TenantId = tenantId,
                WorkspaceId = workspaceId,
                ProjectId = projectId,
                PolicyPackId = packId,
                PolicyPackVersion = "9.9.9",
                ScopeLevel = GovernanceScopeLevel.Project,
                AssignedUtc = DateTime.UtcNow,
            },
            CancellationToken.None);

        EffectiveGovernanceResolver resolver = new(assignmentRepo, packRepo, versionRepo);
        EffectiveGovernanceResolutionResult result = await resolver.ResolveAsync(tenantId, workspaceId, projectId, CancellationToken.None);

        result.Notes.Should()
            .Contain(n => n.Contains("version '9.9.9' not found", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task ResolveAsync_Skips_assignment_when_content_json_is_not_valid_json()
    {
        Guid tenantId = Guid.NewGuid();
        Guid workspaceId = Guid.NewGuid();
        Guid projectId = Guid.NewGuid();
        Guid packId = Guid.NewGuid();

        InMemoryPolicyPackRepository packRepo = new();
        InMemoryPolicyPackVersionRepository versionRepo = new();
        InMemoryPolicyPackAssignmentRepository assignmentRepo = new();

        await packRepo.CreateAsync(
            new PolicyPack
            {
                PolicyPackId = packId,
                TenantId = tenantId,
                WorkspaceId = workspaceId,
                ProjectId = projectId,
                Name = "Bad json",
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
                PolicyPackId = packId,
                Version = "1.0.0",
                ContentJson = "{ not json",
                CreatedUtc = DateTime.UtcNow,
                IsPublished = true,
            },
            CancellationToken.None);

        await assignmentRepo.CreateAsync(
            new PolicyPackAssignment
            {
                TenantId = tenantId,
                WorkspaceId = workspaceId,
                ProjectId = projectId,
                PolicyPackId = packId,
                PolicyPackVersion = "1.0.0",
                ScopeLevel = GovernanceScopeLevel.Project,
                AssignedUtc = DateTime.UtcNow,
            },
            CancellationToken.None);

        EffectiveGovernanceResolver resolver = new(assignmentRepo, packRepo, versionRepo);
        EffectiveGovernanceResolutionResult result = await resolver.ResolveAsync(tenantId, workspaceId, projectId, CancellationToken.None);

        result.Notes.Should()
            .Contain(n => n.Contains("content JSON is corrupt", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task ResolveAsync_Skips_assignment_when_content_json_is_json_null()
    {
        Guid tenantId = Guid.NewGuid();
        Guid workspaceId = Guid.NewGuid();
        Guid projectId = Guid.NewGuid();
        Guid packId = Guid.NewGuid();

        InMemoryPolicyPackRepository packRepo = new();
        InMemoryPolicyPackVersionRepository versionRepo = new();
        InMemoryPolicyPackAssignmentRepository assignmentRepo = new();

        await packRepo.CreateAsync(
            new PolicyPack
            {
                PolicyPackId = packId,
                TenantId = tenantId,
                WorkspaceId = workspaceId,
                ProjectId = projectId,
                Name = "Null content",
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
                PolicyPackId = packId,
                Version = "1.0.0",
                ContentJson = "null",
                CreatedUtc = DateTime.UtcNow,
                IsPublished = true,
            },
            CancellationToken.None);

        await assignmentRepo.CreateAsync(
            new PolicyPackAssignment
            {
                TenantId = tenantId,
                WorkspaceId = workspaceId,
                ProjectId = projectId,
                PolicyPackId = packId,
                PolicyPackVersion = "1.0.0",
                ScopeLevel = GovernanceScopeLevel.Project,
                AssignedUtc = DateTime.UtcNow,
            },
            CancellationToken.None);

        EffectiveGovernanceResolver resolver = new(assignmentRepo, packRepo, versionRepo);
        EffectiveGovernanceResolutionResult result = await resolver.ResolveAsync(tenantId, workspaceId, projectId, CancellationToken.None);

        result.Notes.Should()
            .Contain(n => n.Contains("deserialized to null", StringComparison.OrdinalIgnoreCase));
    }
}
