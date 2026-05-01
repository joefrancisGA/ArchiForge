using ArchLucid.Core.Scim.Models;
using ArchLucid.Persistence.Scim;

namespace ArchLucid.Persistence.Tests.Scim;

public sealed class InMemoryScimTenantTokenRepositoryTests
{
    [SkippableFact]
    public async Task Insert_List_Find_round_trip()
    {
        InMemoryScimTenantTokenRepository sut = new();
        Guid tenantId = Guid.NewGuid();
        byte[] hash = [1, 2, 3];

        Guid id = await sut.InsertAsync(tenantId, "pub-key", hash, CancellationToken.None);

        ScimTokenRow? found = await sut.FindActiveByPublicLookupKeyAsync("pub-key", CancellationToken.None);

        found.Should().NotBeNull();
        found.Id.Should().Be(id);
        found.SecretHash.Should().BeEquivalentTo(hash);

        IReadOnlyList<ScimTokenSummaryRow> list = await sut.ListForTenantAsync(tenantId, CancellationToken.None);

        list.Should().ContainSingle(s => s.PublicLookupKey == "pub-key");
    }

    [SkippableFact]
    public async Task Insert_duplicate_public_key_throws()
    {
        InMemoryScimTenantTokenRepository sut = new();
        Guid tenantId = Guid.NewGuid();

        await sut.InsertAsync(tenantId, "k", [1], CancellationToken.None);

        Func<Task> act = async () => await sut.InsertAsync(tenantId, "k", [2], CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [SkippableFact]
    public async Task FindActive_returns_null_when_revoked()
    {
        InMemoryScimTenantTokenRepository sut = new();
        Guid tenantId = Guid.NewGuid();
        Guid id = await sut.InsertAsync(tenantId, "k", [1], CancellationToken.None);

        await sut.TryRevokeByIdAsync(tenantId, id, CancellationToken.None);

        ScimTokenRow? found = await sut.FindActiveByPublicLookupKeyAsync("k", CancellationToken.None);

        found.Should().BeNull();
    }

    [SkippableFact]
    public async Task TryRevokeByIdAsync_false_for_unknown()
    {
        InMemoryScimTenantTokenRepository sut = new();

        bool ok = await sut.TryRevokeByIdAsync(Guid.NewGuid(), Guid.NewGuid(), CancellationToken.None);

        ok.Should().BeFalse();
    }

    [SkippableFact]
    public async Task TryRevokeByIdAsync_false_when_already_revoked()
    {
        InMemoryScimTenantTokenRepository sut = new();
        Guid tenantId = Guid.NewGuid();
        Guid id = await sut.InsertAsync(tenantId, "k", [1], CancellationToken.None);

        await sut.TryRevokeByIdAsync(tenantId, id, CancellationToken.None);
        bool second = await sut.TryRevokeByIdAsync(tenantId, id, CancellationToken.None);

        second.Should().BeFalse();
    }
}
