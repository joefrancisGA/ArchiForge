using ArchiForge.Persistence.Data.Repositories;

namespace ArchiForge.Persistence.Tests.Contracts;

[Collection(nameof(SqlServerPersistenceCollection))]
[Trait("Category", "SqlServerContainer")]
public sealed class DapperGovernancePromotionRecordRepositoryContractTests(SqlServerPersistenceFixture fixture)
    : GovernancePromotionRecordRepositoryContractTests
{
    protected override void SkipIfSqlServerUnavailable()
    {
        Skip.IfNot(fixture.IsSqlServerAvailable, SqlServerPersistenceFixture.SqlServerUnavailableSkipReason);
    }

    protected override IGovernancePromotionRecordRepository CreateRepository()
    {
        return new GovernancePromotionRecordRepository(new TestSqlDbConnectionFactory(fixture.ConnectionString));
    }
}
