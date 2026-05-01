using ArchLucid.Decisioning.Alerts;
using ArchLucid.Persistence.Connections;

namespace ArchLucid.Persistence.Tests;

/// <summary>
///     <see cref="DapperAlertRuleRepository" /> against real SQL Server (Docker) + production-shaped DDL from DbUp.
/// </summary>
[Collection(nameof(SqlServerPersistenceCollection))]
[Trait("Category", "SqlServerContainer")]
public sealed class DapperAlertRuleRepositorySqlIntegrationTests(SqlServerPersistenceFixture fixture)
{
    [Fact]
    public async Task Create_Get_Update_ListByScope_round_trips_on_sql_server()
    {
        Assert.SkipUnless(fixture.IsSqlServerAvailable, SqlServerPersistenceFixture.SqlServerUnavailableSkipReason);
        SqlConnectionFactory factory = new(fixture.ConnectionString);
        DapperAlertRuleRepository repository = new(factory);

        Guid tenantId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        Guid workspaceId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        Guid projectId = Guid.Parse("33333333-3333-3333-3333-333333333333");
        Guid ruleId = Guid.Parse("44444444-4444-4444-4444-444444444444");

        AlertRule rule = new()
        {
            RuleId = ruleId,
            TenantId = tenantId,
            WorkspaceId = workspaceId,
            ProjectId = projectId,
            Name = "SQL integration rule",
            RuleType = "TestMetric",
            Severity = AlertSeverity.Warning,
            ThresholdValue = 42.5m,
            IsEnabled = true,
            TargetChannelType = "DigestOnly",
            MetadataJson = """{"k":"v"}""",
            CreatedUtc = new DateTime(2026, 3, 27, 11, 0, 0, DateTimeKind.Utc)
        };

        await repository.CreateAsync(rule, CancellationToken.None);

        AlertRule? loaded = await repository.GetByIdAsync(ruleId, CancellationToken.None);
        loaded.Should().NotBeNull();
        loaded.Name.Should().Be("SQL integration rule");
        loaded.ThresholdValue.Should().Be(42.5m);

        rule.Name = "SQL integration rule (updated)";
        await repository.UpdateAsync(rule, CancellationToken.None);

        AlertRule? afterUpdate = await repository.GetByIdAsync(ruleId, CancellationToken.None);
        afterUpdate.Should().NotBeNull();
        afterUpdate.Name.Should().Be("SQL integration rule (updated)");

        IReadOnlyList<AlertRule> list = await repository.ListByScopeAsync(
            tenantId,
            workspaceId,
            projectId,
            CancellationToken.None);

        list.Should().ContainSingle(r => r.RuleId == ruleId);

        IReadOnlyList<AlertRule> enabled = await repository.ListEnabledByScopeAsync(
            tenantId,
            workspaceId,
            projectId,
            CancellationToken.None);

        enabled.Should().Contain(r => r.RuleId == ruleId);
    }
}
