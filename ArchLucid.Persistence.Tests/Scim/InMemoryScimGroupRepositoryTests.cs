using ArchLucid.Core.Scim.Models;
using ArchLucid.Persistence.Scim;

namespace ArchLucid.Persistence.Tests.Scim;

public sealed class InMemoryScimGroupRepositoryTests
{
    [Fact]
    public async Task Insert_Get_List_pagination()
    {
        InMemoryScimGroupRepository sut = new();
        Guid tenantId = Guid.NewGuid();

        await sut.InsertAsync(tenantId, "g1", "Group 1", CancellationToken.None);
        await Task.Delay(15);
        await sut.InsertAsync(tenantId, "g2", "Group 2", CancellationToken.None);

        (IReadOnlyList<ScimGroupRecord> items, int total) =
            await sut.ListAsync(tenantId, startIndex1Based: 2, count: 1, CancellationToken.None);

        total.Should().Be(2);
        items.Should().ContainSingle();
        items[0].ExternalId.Should().Be("g2");
    }

    [Fact]
    public async Task GetById_wrong_tenant_returns_null()
    {
        InMemoryScimGroupRepository sut = new();
        Guid tenantId = Guid.NewGuid();
        ScimGroupRecord g = await sut.InsertAsync(tenantId, "e", "n", CancellationToken.None);

        ScimGroupRecord? row = await sut.GetByIdAsync(Guid.NewGuid(), g.Id, CancellationToken.None);

        row.Should().BeNull();
    }

    [Fact]
    public async Task ReplaceAsync_updates_display_name()
    {
        InMemoryScimGroupRepository sut = new();
        Guid tenantId = Guid.NewGuid();
        ScimGroupRecord g = await sut.InsertAsync(tenantId, "e", "Old", CancellationToken.None);

        await sut.ReplaceAsync(tenantId, g.Id, "e2", "New", CancellationToken.None);

        ScimGroupRecord? row = await sut.GetByIdAsync(tenantId, g.Id, CancellationToken.None);

        row!.DisplayName.Should().Be("New");
        row.ExternalId.Should().Be("e2");
    }

    [Fact]
    public async Task ReplaceAsync_wrong_tenant_is_noop()
    {
        InMemoryScimGroupRepository sut = new();
        Guid tenantId = Guid.NewGuid();
        ScimGroupRecord g = await sut.InsertAsync(tenantId, "e", "n", CancellationToken.None);

        await sut.ReplaceAsync(Guid.NewGuid(), g.Id, "x", "y", CancellationToken.None);

        ScimGroupRecord? row = await sut.GetByIdAsync(tenantId, g.Id, CancellationToken.None);

        row!.DisplayName.Should().Be("n");
    }

    [Fact]
    public async Task SetMembersAsync_replaces_membership()
    {
        InMemoryScimGroupRepository sut = new();
        Guid tenantId = Guid.NewGuid();
        ScimGroupRecord g = await sut.InsertAsync(tenantId, "e", "n", CancellationToken.None);
        Guid u1 = Guid.NewGuid();
        Guid u2 = Guid.NewGuid();

        await sut.SetMembersAsync(tenantId, g.Id, [u1], CancellationToken.None);
        await sut.SetMembersAsync(tenantId, g.Id, [u2], CancellationToken.None);
        await sut.SetMembersAsync(tenantId, g.Id, [], CancellationToken.None);
    }

    [Fact]
    public async Task ListMemberUserIdsAsync_reflects_SetMembersAsync()
    {
        InMemoryScimGroupRepository sut = new();
        Guid tenantId = Guid.NewGuid();
        ScimGroupRecord g = await sut.InsertAsync(tenantId, "e", "n", CancellationToken.None);
        Guid u1 = Guid.NewGuid();
        Guid u2 = Guid.NewGuid();

        IReadOnlyList<Guid> empty = await sut.ListMemberUserIdsAsync(tenantId, g.Id, CancellationToken.None);

        empty.Should().BeEmpty();

        await sut.SetMembersAsync(tenantId, g.Id, [u2, u1], CancellationToken.None);

        IReadOnlyList<Guid> two = await sut.ListMemberUserIdsAsync(tenantId, g.Id, CancellationToken.None);

        two.Should().BeEquivalentTo(new[] { u1, u2 });
    }
}
