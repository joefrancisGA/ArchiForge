using ArchLucid.Decisioning.Alerts.Delivery;

namespace ArchLucid.Persistence.Tests.Contracts;

[Collection(nameof(SqlServerPersistenceCollection))]
[Trait("Category", "SqlServerContainer")]
public sealed class DapperAlertDeliveryAttemptRepositoryContractTests(SqlServerPersistenceFixture fixture)
    : AlertDeliveryAttemptRepositoryContractTests
{
    protected override void SkipIfSqlServerUnavailable()
    {
        Skip.IfNot(fixture.IsSqlServerAvailable, SqlServerPersistenceFixture.SqlServerUnavailableSkipReason);
    }

    protected override IAlertDeliveryAttemptRepository CreateRepository()
    {
        return new DapperAlertDeliveryAttemptRepository(new TestSqlConnectionFactory(fixture.ConnectionString));
    }
}
