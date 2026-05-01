using ArchLucid.Persistence.Connections;

namespace ArchLucid.Persistence.Tests.Contracts;

/// <summary>
///     Runs <see cref="ArchitectureDigestRepositoryContractTests" /> against
///     <see cref="DapperArchitectureDigestRepository" />
///     backed by a real SQL Server instance (<see cref="SqlServerPersistenceFixture" />).
/// </summary>
[Collection(nameof(SqlServerPersistenceCollection))]
[Trait("Category", "SqlServerContainer")]
public sealed class DapperArchitectureDigestRepositoryContractTests(SqlServerPersistenceFixture fixture)
    : ArchitectureDigestRepositoryContractTests
{
    protected override void SkipIfSqlServerUnavailable()
    {
        Assert.SkipUnless(fixture.IsSqlServerAvailable, SqlServerPersistenceFixture.SqlServerUnavailableSkipReason);
    }

    protected override IArchitectureDigestRepository CreateRepository()
    {
        return new DapperArchitectureDigestRepository(new SqlConnectionFactory(fixture.ConnectionString));
    }
}
