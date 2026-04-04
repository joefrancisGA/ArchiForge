using ArchiForge.Persistence.Data.Repositories;

namespace ArchiForge.Persistence.Tests.Contracts;

[Collection(nameof(SqlServerPersistenceCollection))]
[Trait("Category", "SqlServerContainer")]
public sealed class DapperArchitectureRequestRepositoryContractTests(SqlServerPersistenceFixture fixture)
    : ArchitectureRequestRepositoryContractTests
{
    protected override void SkipIfSqlServerUnavailable()
    {
        Skip.IfNot(fixture.IsSqlServerAvailable, SqlServerPersistenceFixture.SqlServerUnavailableSkipReason);
    }

    protected override IArchitectureRequestRepository CreateRepository()
    {
        return new ArchitectureRequestRepository(new TestSqlDbConnectionFactory(fixture.ConnectionString));
    }
}
