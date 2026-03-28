using ArchiForge.Decisioning.Alerts;
using ArchiForge.Persistence.Alerts;
using ArchiForge.Persistence.Connections;

namespace ArchiForge.Persistence.Tests.Contracts;

/// <summary>
/// Runs <see cref="AlertRuleRepositoryContractTests"/> against <see cref="DapperAlertRuleRepository"/>
/// backed by a real SQL Server container (<see cref="SqlServerPersistenceFixture"/>).
/// </summary>
[Collection(nameof(SqlServerPersistenceCollection))]
[Trait("Category", "SqlServerContainer")]
public sealed class DapperAlertRuleRepositoryContractTests(SqlServerPersistenceFixture fixture)
    : AlertRuleRepositoryContractTests
{
    protected override IAlertRuleRepository CreateRepository()
    {
        return new DapperAlertRuleRepository(new SqlConnectionFactory(fixture.ConnectionString));
    }
}
