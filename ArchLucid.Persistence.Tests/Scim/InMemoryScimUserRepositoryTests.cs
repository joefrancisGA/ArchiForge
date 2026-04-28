using ArchLucid.Core.Scim.Filtering;
using ArchLucid.Core.Scim.Models;
using ArchLucid.Persistence.Scim;

namespace ArchLucid.Persistence.Tests.Scim;

public sealed class InMemoryScimUserRepositoryTests
{
    [Fact]
    public async Task Insert_GetById_GetByExternalId_round_trip()
    {
        InMemoryScimUserRepository sut = new();
        Guid tenantId = Guid.NewGuid();

        ScimUserRecord inserted = await sut.InsertAsync(
            tenantId,
            "ext-1",
            "alice",
            "Alice",
            true,
            "Admin",
            CancellationToken.None);

        ScimUserRecord? byId = await sut.GetByIdAsync(tenantId, inserted.Id, CancellationToken.None);
        ScimUserRecord? byExt = await sut.GetByExternalIdAsync(tenantId, "EXT-1", CancellationToken.None);

        byId.Should().NotBeNull();
        byId!.UserName.Should().Be("alice");
        byExt.Should().BeEquivalentTo(byId);
    }

    [Fact]
    public async Task GetById_wrong_tenant_returns_null()
    {
        InMemoryScimUserRepository sut = new();
        Guid tenantId = Guid.NewGuid();
        ScimUserRecord inserted = await sut.InsertAsync(
            tenantId,
            "e",
            "u",
            null,
            true,
            null,
            CancellationToken.None);

        ScimUserRecord? row = await sut.GetByIdAsync(Guid.NewGuid(), inserted.Id, CancellationToken.None);

        row.Should().BeNull();
    }

    [Fact]
    public async Task ReplaceAsync_updates_row()
    {
        InMemoryScimUserRepository sut = new();
        Guid tenantId = Guid.NewGuid();
        ScimUserRecord inserted = await sut.InsertAsync(
            tenantId,
            "e",
            "u",
            null,
            true,
            "Admin",
            CancellationToken.None);

        await sut.ReplaceAsync(
            tenantId,
            inserted.Id,
            "e2",
            "u2",
            "D",
            false,
            "Reader",
            CancellationToken.None);

        ScimUserRecord? row = await sut.GetByIdAsync(tenantId, inserted.Id, CancellationToken.None);

        row!.ExternalId.Should().Be("e2");
        row.UserName.Should().Be("u2");
        row.DisplayName.Should().Be("D");
        row.Active.Should().BeFalse();
        row.ResolvedRole.Should().Be("Reader");
    }

    [Fact]
    public async Task ReplaceAsync_wrong_tenant_is_noop()
    {
        InMemoryScimUserRepository sut = new();
        Guid tenantId = Guid.NewGuid();
        ScimUserRecord inserted = await sut.InsertAsync(
            tenantId,
            "e",
            "u",
            null,
            true,
            null,
            CancellationToken.None);

        await sut.ReplaceAsync(Guid.NewGuid(), inserted.Id, "x", "y", null, true, null, CancellationToken.None);

        ScimUserRecord? row = await sut.GetByIdAsync(tenantId, inserted.Id, CancellationToken.None);

        row!.UserName.Should().Be("u");
    }

    [Fact]
    public async Task PatchAsync_merges_partial_fields()
    {
        InMemoryScimUserRepository sut = new();
        Guid tenantId = Guid.NewGuid();
        ScimUserRecord inserted = await sut.InsertAsync(
            tenantId,
            "e",
            "u",
            "Old",
            true,
            "Admin",
            CancellationToken.None);

        await sut.PatchAsync(tenantId, inserted.Id, null, "newname", null, null, null, CancellationToken.None);

        ScimUserRecord? row = await sut.GetByIdAsync(tenantId, inserted.Id, CancellationToken.None);

        row!.UserName.Should().Be("newname");
        row.DisplayName.Should().Be("Old");
        row.ResolvedRole.Should().Be("Admin");
    }

    [Fact]
    public async Task PatchAsync_wrong_tenant_is_noop()
    {
        InMemoryScimUserRepository sut = new();
        Guid tenantId = Guid.NewGuid();
        ScimUserRecord inserted = await sut.InsertAsync(
            tenantId,
            "e",
            "u",
            null,
            true,
            null,
            CancellationToken.None);

        await sut.PatchAsync(Guid.NewGuid(), inserted.Id, null, "x", null, null, null, CancellationToken.None);

        ScimUserRecord? row = await sut.GetByIdAsync(tenantId, inserted.Id, CancellationToken.None);

        row!.UserName.Should().Be("u");
    }

    [Fact]
    public async Task DeactivateAsync_sets_active_false()
    {
        InMemoryScimUserRepository sut = new();
        Guid tenantId = Guid.NewGuid();
        ScimUserRecord inserted = await sut.InsertAsync(
            tenantId,
            "e",
            "u",
            null,
            true,
            null,
            CancellationToken.None);

        await sut.DeactivateAsync(tenantId, inserted.Id, CancellationToken.None);

        ScimUserRecord? row = await sut.GetByIdAsync(tenantId, inserted.Id, CancellationToken.None);

        row!.Active.Should().BeFalse();
    }

    [Fact]
    public async Task ListAsync_applies_filter_and_pagination()
    {
        InMemoryScimUserRepository sut = new();
        Guid tenantId = Guid.NewGuid();
        await sut.InsertAsync(tenantId, "a", "user1", null, true, null, CancellationToken.None);
        await sut.InsertAsync(tenantId, "b", "user2", null, false, null, CancellationToken.None);

        ScimFilterNode filter = new ScimComparisonNode("userName", "eq", "user2");
        (IReadOnlyList<ScimUserRecord> items, int total) =
            await sut.ListAsync(tenantId, filter, startIndex1Based: 1, count: 10, CancellationToken.None);

        total.Should().Be(1);
        items.Should().ContainSingle(u => u.UserName == "user2");
    }

    [Fact]
    public async Task ListGroupKeysForUserAsync_returns_empty()
    {
        InMemoryScimUserRepository sut = new();

        IReadOnlyList<(string DisplayName, string ExternalId)> keys =
            await sut.ListGroupKeysForUserAsync(Guid.NewGuid(), Guid.NewGuid(), CancellationToken.None);

        keys.Should().BeEmpty();
    }
}
