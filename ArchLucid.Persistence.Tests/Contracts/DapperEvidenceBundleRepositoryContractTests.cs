using ArchLucid.Persistence.Data.Repositories;

namespace ArchLucid.Persistence.Tests.Contracts;

[Collection(nameof(SqlServerPersistenceCollection))]
[Trait("Category", "SqlServerContainer")]
public sealed class DapperEvidenceBundleRepositoryContractTests(SqlServerPersistenceFixture fixture)
    : EvidenceBundleRepositoryContractTests
{
    protected override void SkipIfSqlServerUnavailable()
    {
        Assert.SkipUnless(fixture.IsSqlServerAvailable, SqlServerPersistenceFixture.SqlServerUnavailableSkipReason);
    }

    protected override IEvidenceBundleRepository CreateRepository()
    {
        return new EvidenceBundleRepository(new TestSqlDbConnectionFactory(fixture.ConnectionString));
    }
}
