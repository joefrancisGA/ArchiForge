using ArchiForge.Decisioning.Alerts;

using FluentAssertions;

namespace ArchiForge.Persistence.Tests.Contracts;

/// <summary>
/// Shared contract assertions for <see cref="IAlertRuleRepository"/>.
/// Subclass once with an InMemory implementation and once with Dapper + Testcontainers
/// to guarantee both behave identically.
/// </summary>
public abstract class AlertRuleRepositoryContractTests
{
    protected abstract IAlertRuleRepository CreateRepository();

    private static readonly Guid TenantId = Guid.Parse("a0a0a0a0-a0a0-a0a0-a0a0-a0a0a0a0a0a0");
    private static readonly Guid WorkspaceId = Guid.Parse("b0b0b0b0-b0b0-b0b0-b0b0-b0b0b0b0b0b0");
    private static readonly Guid ProjectId = Guid.Parse("c0c0c0c0-c0c0-c0c0-c0c0-c0c0c0c0c0c0");

    private AlertRule CreateRule(Guid? ruleId = null, bool isEnabled = true, DateTime? createdUtc = null)
    {
        return new AlertRule
        {
            RuleId = ruleId ?? Guid.NewGuid(),
            TenantId = TenantId,
            WorkspaceId = WorkspaceId,
            ProjectId = ProjectId,
            Name = $"Rule-{Guid.NewGuid():N}",
            RuleType = AlertRuleType.CriticalRecommendationCount,
            Severity = AlertSeverity.Warning,
            ThresholdValue = 5m,
            IsEnabled = isEnabled,
            TargetChannelType = "DigestOnly",
            MetadataJson = """{"contract":"test"}""",
            CreatedUtc = createdUtc ?? DateTime.UtcNow
        };
    }

    [Fact]
    public async Task Create_then_GetById_returns_same_rule()
    {
        IAlertRuleRepository repo = CreateRepository();
        AlertRule rule = CreateRule();

        await repo.CreateAsync(rule, CancellationToken.None);

        AlertRule? loaded = await repo.GetByIdAsync(rule.RuleId, CancellationToken.None);

        loaded.Should().NotBeNull();
        loaded!.RuleId.Should().Be(rule.RuleId);
        loaded.Name.Should().Be(rule.Name);
        loaded.RuleType.Should().Be(rule.RuleType);
        loaded.Severity.Should().Be(rule.Severity);
        loaded.ThresholdValue.Should().Be(rule.ThresholdValue);
        loaded.IsEnabled.Should().Be(rule.IsEnabled);
        loaded.TargetChannelType.Should().Be(rule.TargetChannelType);
        loaded.MetadataJson.Should().Be(rule.MetadataJson);
    }

    [Fact]
    public async Task GetById_nonexistent_returns_null()
    {
        IAlertRuleRepository repo = CreateRepository();

        AlertRule? result = await repo.GetByIdAsync(Guid.NewGuid(), CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task Update_modifies_mutable_fields()
    {
        IAlertRuleRepository repo = CreateRepository();
        AlertRule rule = CreateRule();

        await repo.CreateAsync(rule, CancellationToken.None);

        rule.Name = "Updated name";
        rule.ThresholdValue = 99m;
        rule.IsEnabled = false;

        await repo.UpdateAsync(rule, CancellationToken.None);

        AlertRule? after = await repo.GetByIdAsync(rule.RuleId, CancellationToken.None);

        after.Should().NotBeNull();
        after!.Name.Should().Be("Updated name");
        after.ThresholdValue.Should().Be(99m);
        after.IsEnabled.Should().BeFalse();
    }

    [Fact]
    public async Task ListByScope_returns_only_matching_scope()
    {
        IAlertRuleRepository repo = CreateRepository();
        AlertRule matching = CreateRule();
        AlertRule otherProject = CreateRule();
        otherProject.ProjectId = Guid.NewGuid();

        await repo.CreateAsync(matching, CancellationToken.None);
        await repo.CreateAsync(otherProject, CancellationToken.None);

        IReadOnlyList<AlertRule> result = await repo.ListByScopeAsync(
            TenantId, WorkspaceId, ProjectId, CancellationToken.None);

        result.Should().Contain(r => r.RuleId == matching.RuleId);
        result.Should().NotContain(r => r.RuleId == otherProject.RuleId);
    }

    [Fact]
    public async Task ListByScope_orders_by_CreatedUtc_descending()
    {
        IAlertRuleRepository repo = CreateRepository();
        AlertRule older = CreateRule(createdUtc: new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        AlertRule newer = CreateRule(createdUtc: new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc));

        await repo.CreateAsync(older, CancellationToken.None);
        await repo.CreateAsync(newer, CancellationToken.None);

        IReadOnlyList<AlertRule> result = await repo.ListByScopeAsync(
            TenantId, WorkspaceId, ProjectId, CancellationToken.None);

        List<AlertRule> ours = result.Where(r => r.RuleId == older.RuleId || r.RuleId == newer.RuleId).ToList();
        ours.Should().HaveCountGreaterThanOrEqualTo(2);
        ours[0].RuleId.Should().Be(newer.RuleId);
        ours[1].RuleId.Should().Be(older.RuleId);
    }

    [Fact]
    public async Task ListEnabledByScope_excludes_disabled_rules()
    {
        IAlertRuleRepository repo = CreateRepository();
        AlertRule enabled = CreateRule(isEnabled: true);
        AlertRule disabled = CreateRule(isEnabled: false);

        await repo.CreateAsync(enabled, CancellationToken.None);
        await repo.CreateAsync(disabled, CancellationToken.None);

        IReadOnlyList<AlertRule> result = await repo.ListEnabledByScopeAsync(
            TenantId, WorkspaceId, ProjectId, CancellationToken.None);

        result.Should().Contain(r => r.RuleId == enabled.RuleId);
        result.Should().NotContain(r => r.RuleId == disabled.RuleId);
    }

    [Fact]
    public async Task ListEnabledByScope_returns_empty_when_none_enabled()
    {
        IAlertRuleRepository repo = CreateRepository();
        // Use a unique scope so no prior data interferes.
        Guid uniqueProject = Guid.NewGuid();
        AlertRule disabled = CreateRule(isEnabled: false);
        disabled.ProjectId = uniqueProject;

        await repo.CreateAsync(disabled, CancellationToken.None);

        IReadOnlyList<AlertRule> result = await repo.ListEnabledByScopeAsync(
            TenantId, WorkspaceId, uniqueProject, CancellationToken.None);

        result.Should().BeEmpty();
    }
}
