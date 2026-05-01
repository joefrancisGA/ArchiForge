using ArchLucid.Persistence.Data.Repositories;

namespace ArchLucid.Persistence.Tests.Contracts;

[Collection(nameof(SqlServerPersistenceCollection))]
[Trait("Category", "SqlServerContainer")]
public sealed class DapperArchitectureRequestRepositoryContractTests(SqlServerPersistenceFixture fixture)
    : ArchitectureRequestRepositoryContractTests
{
    protected override void SkipIfSqlServerUnavailable()
    {
        Assert.SkipUnless(fixture.IsSqlServerAvailable, SqlServerPersistenceFixture.SqlServerUnavailableSkipReason);
    }

    protected override IArchitectureRequestRepository CreateRepository()
    {
        return new ArchitectureRequestRepository(new TestSqlDbConnectionFactory(fixture.ConnectionString));
    }
}
