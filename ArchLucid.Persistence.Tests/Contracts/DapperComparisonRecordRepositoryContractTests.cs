using ArchLucid.Persistence.Data.Repositories;

namespace ArchLucid.Persistence.Tests.Contracts;

/// <summary>
///     Runs <see cref="ComparisonRecordRepositoryContractTests" /> against <see cref="ComparisonRecordRepository" />.
/// </summary>
[Collection(nameof(SqlServerPersistenceCollection))]
[Trait("Category", "SqlServerContainer")]
public sealed class DapperComparisonRecordRepositoryContractTests(SqlServerPersistenceFixture fixture)
    : ComparisonRecordRepositoryContractTests
{
    protected override void SkipIfSqlServerUnavailable()
    {
        Assert.SkipUnless(fixture.IsSqlServerAvailable, SqlServerPersistenceFixture.SqlServerUnavailableSkipReason);
    }

    protected override IComparisonRecordRepository CreateRepository()
    {
        return new ComparisonRecordRepository(new TestSqlDbConnectionFactory(fixture.ConnectionString));
    }
}
