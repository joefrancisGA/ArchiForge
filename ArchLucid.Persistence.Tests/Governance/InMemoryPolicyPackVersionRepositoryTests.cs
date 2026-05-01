using ArchLucid.Decisioning.Governance.PolicyPacks;
using ArchLucid.Persistence.Governance;

namespace ArchLucid.Persistence.Tests.Governance;

public sealed class InMemoryPolicyPackVersionRepositoryTests
{
    [SkippableFact]
    public async Task CreateAsync_null_throws()
    {
        InMemoryPolicyPackVersionRepository sut = new();

        Func<Task> act = async () => await sut.CreateAsync(null!, CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [SkippableFact]
    public async Task GetByPackAndVersionAsync_finds_row()
    {
        InMemoryPolicyPackVersionRepository sut = new();
        Guid packId = Guid.NewGuid();
        PolicyPackVersion v = new()
        {
            PolicyPackId = packId,
            Version = "1.0.0",
            ContentJson = "{}",
            IsPublished = false
        };

        await sut.CreateAsync(v, CancellationToken.None);

        PolicyPackVersion? found = await sut.GetByPackAndVersionAsync(packId, "1.0.0", CancellationToken.None);

        found.Should().NotBeNull();
        found.ContentJson.Should().Be("{}");
    }

    [SkippableFact]
    public async Task UpdateAsync_replaces_matching_id()
    {
        InMemoryPolicyPackVersionRepository sut = new();
        PolicyPackVersion v = new()
        {
            PolicyPackVersionId = Guid.NewGuid(),
            PolicyPackId = Guid.NewGuid(),
            Version = "1.0.0",
            ContentJson = "a",
            IsPublished = false
        };

        await sut.CreateAsync(v, CancellationToken.None);
        v.ContentJson = "b";
        await sut.UpdateAsync(v, CancellationToken.None);

        PolicyPackVersion? found =
            await sut.GetByPackAndVersionAsync(v.PolicyPackId, "1.0.0", CancellationToken.None);

        found!.ContentJson.Should().Be("b");
    }

    [SkippableFact]
    public async Task UpsertPublishedVersionAsync_insert_then_update_returns_previous_content()
    {
        InMemoryPolicyPackVersionRepository sut = new();
        Guid packId = Guid.NewGuid();

        (PolicyPackVersion first, string? prev1) =
            await sut.UpsertPublishedVersionAsync(packId, "1.0.0", "{}", CancellationToken.None);

        prev1.Should().BeNull();
        first.IsPublished.Should().BeTrue();

        (PolicyPackVersion second, string? prev2) =
            await sut.UpsertPublishedVersionAsync(packId, "1.0.0", "{\"x\":1}", CancellationToken.None);

        second.ContentJson.Should().Be("{\"x\":1}");
        prev2.Should().Be("{}");
    }

    [SkippableFact]
    public async Task ListByPackAsync_orders_newest_first()
    {
        InMemoryPolicyPackVersionRepository sut = new();
        Guid packId = Guid.NewGuid();
        DateTime older = DateTime.UtcNow.AddHours(-2);
        DateTime newer = DateTime.UtcNow.AddHours(-1);

        await sut.CreateAsync(
            new PolicyPackVersion
            {
                PolicyPackId = packId,
                Version = "0.9.0",
                ContentJson = "{}",
                CreatedUtc = older,
                IsPublished = true
            },
            CancellationToken.None);

        await sut.CreateAsync(
            new PolicyPackVersion
            {
                PolicyPackId = packId,
                Version = "1.0.0",
                ContentJson = "{}",
                CreatedUtc = newer,
                IsPublished = true
            },
            CancellationToken.None);

        IReadOnlyList<PolicyPackVersion> list = await sut.ListByPackAsync(packId, CancellationToken.None);

        list[0].Version.Should().Be("1.0.0");
        list[1].Version.Should().Be("0.9.0");
    }
}
