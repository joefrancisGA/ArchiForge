using ArchiForge.Decisioning.Alerts.Delivery;
using ArchiForge.Persistence.Alerts;

using FluentAssertions;

namespace ArchiForge.Persistence.Tests.Alerts;

[Trait("Category", "Unit")]
[Trait("Suite", "Persistence")]
public sealed class InMemoryAlertRoutingSubscriptionRepositoryTests
{
    private static readonly Guid TenantId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    private static readonly Guid WorkspaceId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    private static readonly Guid ProjectId = Guid.Parse("33333333-3333-3333-3333-333333333333");

    private static readonly DateTime BaseUtc = new(2026, 4, 1, 8, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task CreateAsync_then_GetByIdAsync_returns_subscription()
    {
        InMemoryAlertRoutingSubscriptionRepository repo = new();
        Guid id = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        AlertRoutingSubscription sub = BuildSubscription(id, isEnabled: true, BaseUtc, destination: "a@b.com");

        await repo.CreateAsync(sub, CancellationToken.None);

        AlertRoutingSubscription? loaded = await repo.GetByIdAsync(id, CancellationToken.None);

        loaded.Should().NotBeNull();
        loaded!.RoutingSubscriptionId.Should().Be(id);
        loaded.Destination.Should().Be("a@b.com");
    }

    [Fact]
    public async Task GetByIdAsync_returns_null_for_unknown_id()
    {
        InMemoryAlertRoutingSubscriptionRepository repo = new();

        AlertRoutingSubscription? loaded = await repo.GetByIdAsync(Guid.Parse("ffffffff-ffff-ffff-ffff-ffffffffffff"), CancellationToken.None);

        loaded.Should().BeNull();
    }

    [Fact]
    public async Task UpdateAsync_replaces_row_when_id_exists()
    {
        InMemoryAlertRoutingSubscriptionRepository repo = new();
        Guid id = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        await repo.CreateAsync(BuildSubscription(id, isEnabled: true, BaseUtc, destination: "old"), CancellationToken.None);

        AlertRoutingSubscription next = BuildSubscription(id, isEnabled: false, BaseUtc, destination: "new");

        await repo.UpdateAsync(next, CancellationToken.None);

        AlertRoutingSubscription? loaded = await repo.GetByIdAsync(id, CancellationToken.None);
        loaded.Should().NotBeNull();
        loaded!.Destination.Should().Be("new");
        loaded.IsEnabled.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateAsync_is_no_op_when_id_not_found()
    {
        InMemoryAlertRoutingSubscriptionRepository repo = new();
        Guid existingId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
        await repo.CreateAsync(BuildSubscription(existingId, isEnabled: true, BaseUtc, destination: "keep"), CancellationToken.None);

        AlertRoutingSubscription ghost = BuildSubscription(
            Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd"),
            isEnabled: true,
            BaseUtc,
            destination: "ghost");

        await repo.UpdateAsync(ghost, CancellationToken.None);

        IReadOnlyList<AlertRoutingSubscription> all =
            await repo.ListByScopeAsync(TenantId, WorkspaceId, ProjectId, CancellationToken.None);

        all.Should().ContainSingle();
        all[0].Destination.Should().Be("keep");
    }

    [Fact]
    public async Task ListByScopeAsync_filters_scope_and_orders_CreatedUtc_desc()
    {
        InMemoryAlertRoutingSubscriptionRepository repo = new();
        await repo.CreateAsync(
            BuildSubscription(Guid.Parse("40000000-0000-0000-0000-000000000001"), true, BaseUtc, "a"),
            CancellationToken.None);

        await repo.CreateAsync(
            BuildSubscription(Guid.Parse("40000000-0000-0000-0000-000000000002"), true, BaseUtc.AddHours(1), "b"),
            CancellationToken.None);

        await repo.CreateAsync(
            BuildSubscription(
                Guid.Parse("40000000-0000-0000-0000-000000000003"),
                true,
                BaseUtc.AddMinutes(30),
                "c",
                tenantId: Guid.Parse("99999999-9999-9999-9999-999999999999")),
            CancellationToken.None);

        IReadOnlyList<AlertRoutingSubscription> list =
            await repo.ListByScopeAsync(TenantId, WorkspaceId, ProjectId, CancellationToken.None);

        list.Should().HaveCount(2);
        list[0].Destination.Should().Be("b");
        list[1].Destination.Should().Be("a");
    }

    [Fact]
    public async Task ListEnabledByScopeAsync_returns_only_IsEnabled_true()
    {
        InMemoryAlertRoutingSubscriptionRepository repo = new();
        await repo.CreateAsync(
            BuildSubscription(Guid.Parse("41000000-0000-0000-0000-000000000001"), true, BaseUtc.AddHours(2), "on"),
            CancellationToken.None);

        await repo.CreateAsync(
            BuildSubscription(Guid.Parse("41000000-0000-0000-0000-000000000002"), false, BaseUtc.AddHours(3), "off"),
            CancellationToken.None);

        IReadOnlyList<AlertRoutingSubscription> enabled =
            await repo.ListEnabledByScopeAsync(TenantId, WorkspaceId, ProjectId, CancellationToken.None);

        enabled.Should().ContainSingle();
        enabled[0].Destination.Should().Be("on");
    }

    private static AlertRoutingSubscription BuildSubscription(
        Guid id,
        bool isEnabled,
        DateTime createdUtc,
        string destination,
        Guid? tenantId = null)
    {
        return new AlertRoutingSubscription
        {
            RoutingSubscriptionId = id,
            TenantId = tenantId ?? TenantId,
            WorkspaceId = WorkspaceId,
            ProjectId = ProjectId,
            ChannelType = "Email",
            Destination = destination,
            IsEnabled = isEnabled,
            CreatedUtc = createdUtc,
        };
    }
}
