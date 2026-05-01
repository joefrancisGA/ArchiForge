using ArchLucid.Decisioning.Governance.PolicyPacks;

namespace ArchLucid.Persistence.Tests.Contracts;

/// <summary>
///     Shared contract assertions for <see cref="IPolicyPackRepository" />.
/// </summary>
public abstract class PolicyPackRepositoryContractTests
{
    protected virtual void SkipIfSqlServerUnavailable()
    {
    }

    protected abstract IPolicyPackRepository CreateRepository();

    private static PolicyPack NewPack(Guid tenantId, Guid workspaceId, Guid projectId, string name)
    {
        return new PolicyPack
        {
            PolicyPackId = Guid.NewGuid(),
            TenantId = tenantId,
            WorkspaceId = workspaceId,
            ProjectId = projectId,
            Name = name,
            Description = "desc",
            PackType = PolicyPackType.BuiltIn,
            Status = PolicyPackStatus.Draft,
            CreatedUtc = DateTime.UtcNow,
            CurrentVersion = "1.0.0"
        };
    }

    [Fact]
    public async Task Create_then_GetById_returns_pack()
    {
        SkipIfSqlServerUnavailable();
        IPolicyPackRepository repo = CreateRepository();
        Guid tenantId = Guid.Parse("f1f1f1f1-f1f1-f1f1-f1f1-f1f1f1f1f1f1");
        Guid workspaceId = Guid.Parse("f2f2f2f2-f2f2-f2f2-f2f2-f2f2f2f2f2f2");
        Guid projectId = Guid.Parse("f3f3f3f3-f3f3-f3f3-f3f3-f3f3f3f3f3f3");
        PolicyPack pack = NewPack(tenantId, workspaceId, projectId, "pack-a");

        await repo.CreateAsync(pack, CancellationToken.None);

        PolicyPack? loaded = await repo.GetByIdAsync(pack.PolicyPackId, CancellationToken.None);
        loaded.Should().NotBeNull();
        loaded.Name.Should().Be("pack-a");
        loaded.TenantId.Should().Be(tenantId);
    }

    [Fact]
    public async Task GetById_unknown_returns_null()
    {
        SkipIfSqlServerUnavailable();
        IPolicyPackRepository repo = CreateRepository();

        PolicyPack? loaded = await repo.GetByIdAsync(Guid.NewGuid(), CancellationToken.None);

        loaded.Should().BeNull();
    }

    [Fact]
    public async Task ListByScope_filters_other_project()
    {
        SkipIfSqlServerUnavailable();
        IPolicyPackRepository repo = CreateRepository();
        Guid tenantId = Guid.Parse("a1a1a1a1-a1a1-a1a1-a1a1-a1a1a1a1a1a1");
        Guid workspaceId = Guid.Parse("a2a2a2a2-a2a2-a2a2-a2a2-a2a2a2a2a2a2");
        Guid projectId = Guid.Parse("a3a3a3a3-a3a3-a3a3-a3a3-a3a3a3a3a3a3");
        PolicyPack matching = NewPack(tenantId, workspaceId, projectId, "in-scope");
        PolicyPack other = NewPack(tenantId, workspaceId, Guid.NewGuid(), "other-proj");

        await repo.CreateAsync(matching, CancellationToken.None);
        await repo.CreateAsync(other, CancellationToken.None);

        IReadOnlyList<PolicyPack> list =
            await repo.ListByScopeAsync(tenantId, workspaceId, projectId, CancellationToken.None);

        list.Should().Contain(x => x.PolicyPackId == matching.PolicyPackId);
        list.Should().NotContain(x => x.PolicyPackId == other.PolicyPackId);
    }

    [Fact]
    public async Task UpdateAsync_modifies_GetById()
    {
        SkipIfSqlServerUnavailable();
        IPolicyPackRepository repo = CreateRepository();
        Guid tenantId = Guid.Parse("b1b1b1b1-b1b1-b1b1-b1b1-b1b1b1b1b1b1");
        Guid workspaceId = Guid.Parse("b2b2b2b2-b2b2-b2b2-b2b2-b2b2b2b2b2b2");
        Guid projectId = Guid.Parse("b3b3b3b3-b3b3-b3b3-b3b3-b3b3b3b3b3b3");
        PolicyPack pack = NewPack(tenantId, workspaceId, projectId, "before");

        await repo.CreateAsync(pack, CancellationToken.None);

        pack.Name = "after";
        pack.Status = PolicyPackStatus.Active;
        await repo.UpdateAsync(pack, CancellationToken.None);

        PolicyPack? loaded = await repo.GetByIdAsync(pack.PolicyPackId, CancellationToken.None);
        loaded.Should().NotBeNull();
        loaded.Name.Should().Be("after");
        loaded.Status.Should().Be(PolicyPackStatus.Active);
    }
}
