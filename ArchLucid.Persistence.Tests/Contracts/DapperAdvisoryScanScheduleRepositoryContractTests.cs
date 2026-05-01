namespace ArchLucid.Persistence.Tests.Contracts;

[Collection(nameof(SqlServerPersistenceCollection))]
[Trait("Category", "SqlServerContainer")]
public sealed class DapperAdvisoryScanScheduleRepositoryContractTests(SqlServerPersistenceFixture fixture)
    : AdvisoryScanScheduleRepositoryContractTests
{
    protected override void SkipIfSqlServerUnavailable()
    {
        Assert.SkipUnless(fixture.IsSqlServerAvailable, SqlServerPersistenceFixture.SqlServerUnavailableSkipReason);
    }

    protected override IAdvisoryScanScheduleRepository CreateRepository()
    {
        return new DapperAdvisoryScanScheduleRepository(new TestSqlConnectionFactory(fixture.ConnectionString));
    }
}
