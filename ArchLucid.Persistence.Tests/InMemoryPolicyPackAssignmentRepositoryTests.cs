using ArchLucid.Decisioning.Governance.PolicyPacks;
using ArchLucid.Decisioning.Governance.Resolution;
using ArchLucid.Persistence.Governance;

namespace ArchLucid.Persistence.Tests;

/// <summary>
///     <see cref="InMemoryPolicyPackAssignmentRepository" /> parity with SQL semantics for archival.
/// </summary>
[Trait("Category", "Unit")]
public sealed class InMemoryPolicyPackAssignmentRepositoryTests
{
    [Fact]
    public async Task ListByScopeAsync_Excludes_archived_rows()
    {
        Guid tenantId = Guid.NewGuid();
        Guid workspaceId = Guid.NewGuid();
        Guid projectId = Guid.NewGuid();
        Guid assignmentId = Guid.NewGuid();

        InMemoryPolicyPackAssignmentRepository sut = new();
        await sut.CreateAsync(
            new PolicyPackAssignment
            {
                AssignmentId = assignmentId,
                TenantId = tenantId,
                WorkspaceId = workspaceId,
                ProjectId = projectId,
                PolicyPackId = Guid.NewGuid(),
                PolicyPackVersion = "1.0.0",
                IsEnabled = true,
                ScopeLevel = GovernanceScopeLevel.Project,
                AssignedUtc = DateTime.UtcNow,
                ArchivedUtc = DateTime.UtcNow
            },
            CancellationToken.None);

        IReadOnlyList<PolicyPackAssignment> rows =
            await sut.ListByScopeAsync(tenantId, workspaceId, projectId, CancellationToken.None);

        rows.Should().BeEmpty();
    }

    [Fact]
    public async Task ArchiveAsync_Sets_flag_and_is_idempotent_for_list()
    {
        Guid tenantId = Guid.NewGuid();
        Guid workspaceId = Guid.NewGuid();
        Guid projectId = Guid.NewGuid();
        Guid assignmentId = Guid.NewGuid();

        InMemoryPolicyPackAssignmentRepository sut = new();
        await sut.CreateAsync(
            new PolicyPackAssignment
            {
                AssignmentId = assignmentId,
                TenantId = tenantId,
                WorkspaceId = workspaceId,
                ProjectId = projectId,
                PolicyPackId = Guid.NewGuid(),
                PolicyPackVersion = "1.0.0",
                IsEnabled = true,
                ScopeLevel = GovernanceScopeLevel.Project,
                AssignedUtc = DateTime.UtcNow
            },
            CancellationToken.None);

        bool first = await sut.ArchiveAsync(tenantId, assignmentId, CancellationToken.None);
        first.Should().BeTrue();

        bool second = await sut.ArchiveAsync(tenantId, assignmentId, CancellationToken.None);
        second.Should().BeFalse();

        IReadOnlyList<PolicyPackAssignment> rows =
            await sut.ListByScopeAsync(tenantId, workspaceId, projectId, CancellationToken.None);
        rows.Should().BeEmpty();
    }
}
