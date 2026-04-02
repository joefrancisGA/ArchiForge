using ArchiForge.Decisioning.Alerts.Composite;
using ArchiForge.Persistence.Alerts;

using FluentAssertions;

namespace ArchiForge.Persistence.Tests.Alerts;

[Trait("Category", "Unit")]
[Trait("Suite", "Persistence")]
public sealed class InMemoryCompositeAlertRuleRepositoryTests
{
    private static readonly Guid TenantId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    private static readonly Guid WorkspaceId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    private static readonly Guid ProjectId = Guid.Parse("33333333-3333-3333-3333-333333333333");

    private static readonly DateTime BaseUtc = new(2026, 4, 2, 10, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task CreateAsync_stores_clone_mutating_original_does_not_change_repository()
    {
        InMemoryCompositeAlertRuleRepository repo = new();
        Guid ruleId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        CompositeAlertRule rule = BuildRule(ruleId, name: "v1", BaseUtc, enabled: true);

        await repo.CreateAsync(rule, CancellationToken.None);
        rule.Name = "mutated";

        CompositeAlertRule? loaded = await repo.GetByIdAsync(ruleId, CancellationToken.None);
        loaded.Should().NotBeNull();
        loaded!.Name.Should().Be("v1");
    }

    [Fact]
    public async Task GetByIdAsync_returns_clone_mutations_do_not_affect_stored_rule()
    {
        InMemoryCompositeAlertRuleRepository repo = new();
        Guid ruleId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        await repo.CreateAsync(BuildRule(ruleId, name: "stable", BaseUtc, enabled: true), CancellationToken.None);

        CompositeAlertRule? first = await repo.GetByIdAsync(ruleId, CancellationToken.None);
        first.Should().NotBeNull();
        first!.Name = "broken";

        CompositeAlertRule? second = await repo.GetByIdAsync(ruleId, CancellationToken.None);
        second.Should().NotBeNull();
        second!.Name.Should().Be("stable");
    }

    [Fact]
    public async Task CreateAsync_clones_condition_list()
    {
        InMemoryCompositeAlertRuleRepository repo = new();
        Guid ruleId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
        Guid conditionId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");
        CompositeAlertRule rule = BuildRule(ruleId, name: "r", BaseUtc, enabled: true);
        rule.Conditions.Add(
            new AlertRuleCondition
            {
                ConditionId = conditionId,
                MetricType = AlertMetricType.CostIncreasePercent,
                Operator = AlertConditionOperator.GreaterThanOrEqual,
                ThresholdValue = 10m,
            });

        await repo.CreateAsync(rule, CancellationToken.None);
        rule.Conditions[0].ThresholdValue = 99m;

        CompositeAlertRule? loaded = await repo.GetByIdAsync(ruleId, CancellationToken.None);
        loaded.Should().NotBeNull();
        loaded!.Conditions.Should().ContainSingle();
        loaded.Conditions[0].ThresholdValue.Should().Be(10m);
    }

    [Fact]
    public async Task UpdateAsync_replaces_by_CompositeRuleId()
    {
        InMemoryCompositeAlertRuleRepository repo = new();
        Guid ruleId = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee");
        await repo.CreateAsync(BuildRule(ruleId, name: "old", BaseUtc, enabled: true), CancellationToken.None);

        CompositeAlertRule next = BuildRule(ruleId, name: "new", BaseUtc.AddHours(1), enabled: false);
        await repo.UpdateAsync(next, CancellationToken.None);

        CompositeAlertRule? loaded = await repo.GetByIdAsync(ruleId, CancellationToken.None);
        loaded.Should().NotBeNull();
        loaded!.Name.Should().Be("new");
        loaded.IsEnabled.Should().BeFalse();
    }

    [Fact]
    public async Task GetByIdAsync_returns_null_for_unknown_id()
    {
        InMemoryCompositeAlertRuleRepository repo = new();

        CompositeAlertRule? loaded = await repo.GetByIdAsync(Guid.Parse("ffffffff-ffff-ffff-ffff-ffffffffffff"), CancellationToken.None);

        loaded.Should().BeNull();
    }

    [Fact]
    public async Task ListByScopeAsync_filters_and_orders_desc_and_returns_clones()
    {
        InMemoryCompositeAlertRuleRepository repo = new();
        await repo.CreateAsync(
            BuildRule(Guid.Parse("50000000-0000-0000-0000-000000000001"), "a", BaseUtc, enabled: true),
            CancellationToken.None);

        await repo.CreateAsync(
            BuildRule(Guid.Parse("50000000-0000-0000-0000-000000000002"), "b", BaseUtc.AddHours(2), enabled: true),
            CancellationToken.None);

        await repo.CreateAsync(
            BuildRule(
                Guid.Parse("50000000-0000-0000-0000-000000000003"),
                "other-tenant",
                BaseUtc.AddHours(5),
                enabled: true,
                tenantId: Guid.Parse("99999999-9999-9999-9999-999999999999")),
            CancellationToken.None);

        IReadOnlyList<CompositeAlertRule> list =
            await repo.ListByScopeAsync(TenantId, WorkspaceId, ProjectId, CancellationToken.None);

        list.Should().HaveCount(2);
        list[0].Name.Should().Be("b");
        list[1].Name.Should().Be("a");

        list[0].Name = "mutate-list";
        CompositeAlertRule? again = await repo.GetByIdAsync(Guid.Parse("50000000-0000-0000-0000-000000000002"), CancellationToken.None);
        again.Should().NotBeNull();
        again!.Name.Should().Be("b");
    }

    [Fact]
    public async Task ListEnabledByScopeAsync_excludes_disabled_rules()
    {
        InMemoryCompositeAlertRuleRepository repo = new();
        await repo.CreateAsync(
            BuildRule(Guid.Parse("51000000-0000-0000-0000-000000000001"), "on", BaseUtc, enabled: true),
            CancellationToken.None);

        await repo.CreateAsync(
            BuildRule(Guid.Parse("51000000-0000-0000-0000-000000000002"), "off", BaseUtc.AddHours(1), enabled: false),
            CancellationToken.None);

        IReadOnlyList<CompositeAlertRule> enabled =
            await repo.ListEnabledByScopeAsync(TenantId, WorkspaceId, ProjectId, CancellationToken.None);

        enabled.Should().ContainSingle();
        enabled[0].Name.Should().Be("on");
    }

    [Fact]
    public async Task CreateAsync_with_null_rule_throws()
    {
        InMemoryCompositeAlertRuleRepository repo = new();

        Func<Task> act = async () => await repo.CreateAsync(null!, CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task UpdateAsync_with_null_rule_throws()
    {
        InMemoryCompositeAlertRuleRepository repo = new();

        Func<Task> act = async () => await repo.UpdateAsync(null!, CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    private static CompositeAlertRule BuildRule(
        Guid compositeRuleId,
        string name,
        DateTime createdUtc,
        bool enabled,
        Guid? tenantId = null)
    {
        return new CompositeAlertRule
        {
            CompositeRuleId = compositeRuleId,
            TenantId = tenantId ?? TenantId,
            WorkspaceId = WorkspaceId,
            ProjectId = ProjectId,
            Name = name,
            IsEnabled = enabled,
            CreatedUtc = createdUtc,
            Conditions = [],
        };
    }
}
