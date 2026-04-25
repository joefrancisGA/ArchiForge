using ArchLucid.Decisioning.Advisory.Delivery;

using FluentAssertions;

namespace ArchLucid.Persistence.Tests.Advisory;

[Trait("Category", "Unit")]
[Trait("Suite", "Persistence")]
public sealed class InMemoryDigestSubscriptionRepositoryTests
{
    private static readonly Guid TenantId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    private static readonly Guid WorkspaceId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    private static readonly Guid ProjectId = Guid.Parse("33333333-3333-3333-3333-333333333333");

    private static readonly DateTime BaseUtc = new(2026, 4, 5, 7, 30, 0, DateTimeKind.Utc);

    [Fact]
    public async Task CreateAsync_then_GetByIdAsync_returns_row()
    {
        InMemoryDigestSubscriptionRepository repo = new();
        Guid id = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        DigestSubscription sub = Build(id, "digest@x.com", true, BaseUtc);

        await repo.CreateAsync(sub, CancellationToken.None);

        DigestSubscription? loaded = await repo.GetByIdAsync(id, CancellationToken.None);

        loaded.Should().NotBeNull();
        loaded.SubscriptionId.Should().Be(id);
        loaded.Destination.Should().Be("digest@x.com");
    }

    [Fact]
    public async Task UpdateAsync_replaces_existing_by_SubscriptionId()
    {
        InMemoryDigestSubscriptionRepository repo = new();
        Guid id = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        await repo.CreateAsync(Build(id, "old", true, BaseUtc), CancellationToken.None);

        DigestSubscription next = Build(id, "new", false, BaseUtc.AddHours(1));
        next.LastDeliveredUtc = BaseUtc.AddHours(2);

        await repo.UpdateAsync(next, CancellationToken.None);

        DigestSubscription? loaded = await repo.GetByIdAsync(id, CancellationToken.None);
        loaded.Should().NotBeNull();
        loaded.Destination.Should().Be("new");
        loaded.IsEnabled.Should().BeFalse();
        loaded.LastDeliveredUtc.Should().Be(BaseUtc.AddHours(2));
    }

    [Fact]
    public async Task GetByIdAsync_returns_null_for_unknown_id()
    {
        InMemoryDigestSubscriptionRepository repo = new();

        DigestSubscription? loaded =
            await repo.GetByIdAsync(Guid.Parse("ffffffff-ffff-ffff-ffff-ffffffffffff"), CancellationToken.None);

        loaded.Should().BeNull();
    }

    [Fact]
    public async Task ListByScopeAsync_filters_and_orders_CreatedUtc_desc()
    {
        InMemoryDigestSubscriptionRepository repo = new();
        await repo.CreateAsync(Build(Guid.Parse("80000000-0000-0000-0000-000000000001"), "a", true, BaseUtc),
            CancellationToken.None);
        await repo.CreateAsync(
            Build(Guid.Parse("80000000-0000-0000-0000-000000000002"), "b", true, BaseUtc.AddHours(2)),
            CancellationToken.None);
        await repo.CreateAsync(
            Build(Guid.Parse("80000000-0000-0000-0000-000000000003"), "c", true, BaseUtc.AddHours(1),
                Guid.Parse("99999999-9999-9999-9999-999999999999")),
            CancellationToken.None);

        IReadOnlyList<DigestSubscription> list =
            await repo.ListByScopeAsync(TenantId, WorkspaceId, ProjectId, CancellationToken.None);

        list.Should().HaveCount(2);
        list[0].Destination.Should().Be("b");
        list[1].Destination.Should().Be("a");
    }

    [Fact]
    public async Task ListEnabledByScopeAsync_returns_only_IsEnabled_true()
    {
        InMemoryDigestSubscriptionRepository repo = new();
        await repo.CreateAsync(
            Build(Guid.Parse("81000000-0000-0000-0000-000000000001"), "on", true, BaseUtc.AddMinutes(5)),
            CancellationToken.None);
        await repo.CreateAsync(
            Build(Guid.Parse("81000000-0000-0000-0000-000000000002"), "off", false, BaseUtc.AddMinutes(10)),
            CancellationToken.None);

        IReadOnlyList<DigestSubscription> enabled =
            await repo.ListEnabledByScopeAsync(TenantId, WorkspaceId, ProjectId, CancellationToken.None);

        enabled.Should().ContainSingle();
        enabled[0].Destination.Should().Be("on");
    }

    [Fact]
    public async Task CreateAsync_trims_oldest_when_exceeding_500()
    {
        InMemoryDigestSubscriptionRepository repo = new();
        Guid firstId = Guid.Parse("82000000-0000-0000-0000-000000000001");
        await repo.CreateAsync(Build(firstId, "first", true, BaseUtc), CancellationToken.None);

        Task[] tail = Enumerable
            .Range(2, 500)
            .Select(i => repo.CreateAsync(
                Build(Guid.Parse($"82000000-0000-0000-0000-{i:000000000000}"), $"d{i}", true, BaseUtc.AddMinutes(i)),
                CancellationToken.None))
            .ToArray();

        await Task.WhenAll(tail);

        DigestSubscription? gone = await repo.GetByIdAsync(firstId, CancellationToken.None);
        gone.Should().BeNull();

        IReadOnlyList<DigestSubscription> scope =
            await repo.ListByScopeAsync(TenantId, WorkspaceId, ProjectId, CancellationToken.None);
        scope.Should().HaveCount(500);
    }

    [Fact]
    public async Task CreateAsync_with_null_subscription_throws()
    {
        InMemoryDigestSubscriptionRepository repo = new();

        Func<Task> act = async () => await repo.CreateAsync(null!, CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task UpdateAsync_with_null_subscription_throws()
    {
        InMemoryDigestSubscriptionRepository repo = new();

        Func<Task> act = async () => await repo.UpdateAsync(null!, CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    private static DigestSubscription Build(
        Guid id,
        string destination,
        bool enabled,
        DateTime createdUtc,
        Guid? tenantId = null)
    {
        return new DigestSubscription
        {
            SubscriptionId = id,
            TenantId = tenantId ?? TenantId,
            WorkspaceId = WorkspaceId,
            ProjectId = ProjectId,
            ChannelType = "Email",
            Destination = destination,
            IsEnabled = enabled,
            CreatedUtc = createdUtc
        };
    }
}
