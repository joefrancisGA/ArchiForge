using ArchiForge.Persistence.Interfaces;
using ArchiForge.Persistence.Repositories;

namespace ArchiForge.Persistence.Tests.Contracts;

/// <summary>
/// Runs <see cref="RunRepositoryContractTests"/> against <see cref="SqlRunRepository"/>.
/// </summary>
[Collection(nameof(SqlServerPersistenceCollection))]
[Trait("Category", "SqlServerContainer")]
public sealed class DapperRunRepositoryContractTests(SqlServerPersistenceFixture fixture) : RunRepositoryContractTests
{
    protected override bool IncludeArchiveRunsCreatedBeforeContractTest => false;

    protected override void SkipIfSqlServerUnavailable()
    {
        Skip.IfNot(fixture.IsSqlServerAvailable, SqlServerPersistenceFixture.SqlServerUnavailableSkipReason);
    }

    protected override IRunRepository CreateRepository()
    {
        TestSqlConnectionFactory sqlFactory = new(fixture.ConnectionString);
        TestAuthorityRunListConnectionFactory listFactory = new(sqlFactory);

        return new SqlRunRepository(sqlFactory, listFactory);
    }
}
