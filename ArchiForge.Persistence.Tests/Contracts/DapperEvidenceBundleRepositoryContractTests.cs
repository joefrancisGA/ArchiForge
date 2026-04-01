using ArchiForge.Data.Repositories;

using ArchiForge.Persistence.Tests.Support;

namespace ArchiForge.Persistence.Tests.Contracts;

[Collection(nameof(SqlServerPersistenceCollection))]
[Trait("Category", "SqlServerContainer")]
public sealed class DapperEvidenceBundleRepositoryContractTests(SqlServerPersistenceFixture fixture)
    : EvidenceBundleRepositoryContractTests
{
    protected override void SkipIfSqlServerUnavailable()
    {
        Skip.IfNot(fixture.IsSqlServerAvailable, SqlServerPersistenceFixture.SqlServerUnavailableSkipReason);
    }

    protected override IEvidenceBundleRepository CreateRepository()
    {
        return new EvidenceBundleRepository(new TestSqlDbConnectionFactory(fixture.ConnectionString));
    }
}
