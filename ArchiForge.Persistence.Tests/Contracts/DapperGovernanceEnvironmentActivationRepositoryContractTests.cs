using ArchiForge.Persistence.Data.Repositories;

namespace ArchiForge.Persistence.Tests.Contracts;

[Collection(nameof(SqlServerPersistenceCollection))]
[Trait("Category", "SqlServerContainer")]
public sealed class DapperGovernanceEnvironmentActivationRepositoryContractTests(SqlServerPersistenceFixture fixture)
    : GovernanceEnvironmentActivationRepositoryContractTests
{
    protected override void SkipIfSqlServerUnavailable()
    {
        Skip.IfNot(fixture.IsSqlServerAvailable, SqlServerPersistenceFixture.SqlServerUnavailableSkipReason);
    }

    protected override IGovernanceEnvironmentActivationRepository CreateRepository()
    {
        return new GovernanceEnvironmentActivationRepository(new TestSqlDbConnectionFactory(fixture.ConnectionString));
    }
}
