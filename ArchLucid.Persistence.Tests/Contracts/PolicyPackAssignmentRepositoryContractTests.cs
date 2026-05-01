using ArchLucid.Decisioning.Governance.PolicyPacks;
using ArchLucid.Decisioning.Governance.Resolution;

namespace ArchLucid.Persistence.Tests.Contracts;

/// <summary>
///     Shared contract assertions for <see cref="IPolicyPackAssignmentRepository" />.
/// </summary>
public abstract class PolicyPackAssignmentRepositoryContractTests
{
    private static readonly Guid TenantA = Guid.Parse("a1a1a1a1-a1a1-a1a1-a1a1-a1a1a1a1a1a1");
    private static readonly Guid WorkspaceW = Guid.Parse("b1b1b1b1-b1b1-b1b1-b1b1-b1b1b1b1b1b1");
    private static readonly Guid WorkspaceOther = Guid.Parse("b2b2b2b2-b2b2-b2b2-b2b2-b2b2b2b2b2b2");
    private static readonly Guid ProjectP = Guid.Parse("c1c1c1c1-c1c1-c1c1-c1c1-c1c1c1c1c1c1");
    protected abstract IPolicyPackAssignmentRepository CreateRepository();

    /// <summary>No-op for in-memory implementations; Dapper + SQL Server subclasses skip when no instance is available.</summary>
    protected virtual void SkipIfSqlServerUnavailable()
    {
    }

    private static PolicyPackAssignment CreateAssignment(
        Guid tenantId,
        Guid workspaceId,
        Guid projectId,
        string scopeLevel,
        Guid? assignmentId = null,
        bool isEnabled = true)
    {
        return new PolicyPackAssignment
        {
            AssignmentId = assignmentId ?? Guid.NewGuid(),
            TenantId = tenantId,
            WorkspaceId = workspaceId,
            ProjectId = projectId,
            PolicyPackId = Guid.NewGuid(),
            PolicyPackVersion = "1.0.0",
            IsEnabled = isEnabled,
            ScopeLevel = scopeLevel,
            IsPinned = false,
            AssignedUtc = DateTime.UtcNow,
            ArchivedUtc = null
        };
    }

    [Fact]
    public async Task Create_then_ListByScope_project_row_visible_in_that_project()
    {
        SkipIfSqlServerUnavailable();
        IPolicyPackAssignmentRepository repo = CreateRepository();
        PolicyPackAssignment row = CreateAssignment(TenantA, WorkspaceW, ProjectP, GovernanceScopeLevel.Project);

        await repo.CreateAsync(row, CancellationToken.None);

        IReadOnlyList<PolicyPackAssignment> list =
            await repo.ListByScopeAsync(TenantA, WorkspaceW, ProjectP, CancellationToken.None);

        list.Should().ContainSingle(a => a.AssignmentId == row.AssignmentId);
    }

    [Fact]
    public async Task Create_tenant_scope_ListByScope_visible_for_any_workspace_in_tenant()
    {
        SkipIfSqlServerUnavailable();
        IPolicyPackAssignmentRepository repo = CreateRepository();
        PolicyPackAssignment row = CreateAssignment(TenantA, Guid.Empty, Guid.Empty, GovernanceScopeLevel.Tenant);

        await repo.CreateAsync(row, CancellationToken.None);

        IReadOnlyList<PolicyPackAssignment> list =
            await repo.ListByScopeAsync(TenantA, WorkspaceOther, ProjectP, CancellationToken.None);

        list.Should().ContainSingle(a => a.AssignmentId == row.AssignmentId);
    }

    [Fact]
    public async Task Create_workspace_scope_ListByScope_requires_matching_workspace()
    {
        SkipIfSqlServerUnavailable();
        IPolicyPackAssignmentRepository repo = CreateRepository();
        PolicyPackAssignment row = CreateAssignment(TenantA, WorkspaceW, Guid.Empty, GovernanceScopeLevel.Workspace);

        await repo.CreateAsync(row, CancellationToken.None);

        IReadOnlyList<PolicyPackAssignment> match =
            await repo.ListByScopeAsync(TenantA, WorkspaceW, ProjectP, CancellationToken.None);

        IReadOnlyList<PolicyPackAssignment> noMatch =
            await repo.ListByScopeAsync(TenantA, WorkspaceOther, ProjectP, CancellationToken.None);

        match.Should().ContainSingle(a => a.AssignmentId == row.AssignmentId);
        noMatch.Should().NotContain(a => a.AssignmentId == row.AssignmentId);
    }

    [Fact]
    public async Task Update_persists_IsEnabled()
    {
        SkipIfSqlServerUnavailable();
        IPolicyPackAssignmentRepository repo = CreateRepository();
        PolicyPackAssignment row = CreateAssignment(TenantA, WorkspaceW, ProjectP, GovernanceScopeLevel.Project);

        await repo.CreateAsync(row, CancellationToken.None);
        row.IsEnabled = false;
        await repo.UpdateAsync(row, CancellationToken.None);

        IReadOnlyList<PolicyPackAssignment> list =
            await repo.ListByScopeAsync(TenantA, WorkspaceW, ProjectP, CancellationToken.None);

        PolicyPackAssignment? updated = list.SingleOrDefault(a => a.AssignmentId == row.AssignmentId);
        updated.Should().NotBeNull();
        updated.IsEnabled.Should().BeFalse();
    }

    [Fact]
    public async Task ArchiveAsync_excludes_row_from_ListByScope()
    {
        SkipIfSqlServerUnavailable();
        IPolicyPackAssignmentRepository repo = CreateRepository();
        PolicyPackAssignment row = CreateAssignment(TenantA, WorkspaceW, ProjectP, GovernanceScopeLevel.Project);

        await repo.CreateAsync(row, CancellationToken.None);
        bool archived = await repo.ArchiveAsync(TenantA, row.AssignmentId, CancellationToken.None);

        archived.Should().BeTrue();

        IReadOnlyList<PolicyPackAssignment> list =
            await repo.ListByScopeAsync(TenantA, WorkspaceW, ProjectP, CancellationToken.None);

        list.Should().NotContain(a => a.AssignmentId == row.AssignmentId);
    }

    [Fact]
    public async Task GetByTenantAndAssignmentId_after_Create_returns_row()
    {
        SkipIfSqlServerUnavailable();
        IPolicyPackAssignmentRepository repo = CreateRepository();
        PolicyPackAssignment row = CreateAssignment(TenantA, WorkspaceW, ProjectP, GovernanceScopeLevel.Project);

        await repo.CreateAsync(row, CancellationToken.None);

        PolicyPackAssignment? found =
            await repo.GetByTenantAndAssignmentIdAsync(TenantA, row.AssignmentId, CancellationToken.None);

        found.Should().NotBeNull();
        found.AssignmentId.Should().Be(row.AssignmentId);
        found.PolicyPackId.Should().Be(row.PolicyPackId);
    }

    [Fact]
    public async Task ArchiveAsync_unknown_assignment_returns_false()
    {
        SkipIfSqlServerUnavailable();
        IPolicyPackAssignmentRepository repo = CreateRepository();

        bool ok = await repo.ArchiveAsync(TenantA, Guid.NewGuid(), CancellationToken.None);

        ok.Should().BeFalse();
    }

    [Fact]
    public async Task ArchiveAsync_wrong_tenant_returns_false()
    {
        SkipIfSqlServerUnavailable();
        IPolicyPackAssignmentRepository repo = CreateRepository();
        PolicyPackAssignment row = CreateAssignment(TenantA, WorkspaceW, ProjectP, GovernanceScopeLevel.Project);

        await repo.CreateAsync(row, CancellationToken.None);

        Guid otherTenant = Guid.Parse("d1d1d1d1-d1d1-d1d1-d1d1-d1d1d1d1d1d1");
        bool ok = await repo.ArchiveAsync(otherTenant, row.AssignmentId, CancellationToken.None);

        ok.Should().BeFalse();

        IReadOnlyList<PolicyPackAssignment> list =
            await repo.ListByScopeAsync(TenantA, WorkspaceW, ProjectP, CancellationToken.None);

        list.Should().ContainSingle(a => a.AssignmentId == row.AssignmentId);
    }
}
