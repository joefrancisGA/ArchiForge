namespace ArchLucid.Persistence.Tests.Contracts;

[Collection(nameof(SqlServerPersistenceCollection))]
[Trait("Category", "SqlServerContainer")]
public sealed class DapperAdvisoryScanScheduleRepositoryContractTests(SqlServerPersistenceFixture fixture)
    : AdvisoryScanScheduleRepositoryContractTests
{
    protected override void SkipIfSqlServerUnavailable()
    {
        Skip.IfNot(fixture.IsSqlServerAvailable, SqlServerPersistenceFixture.SqlServerUnavailableSkipReason);
    }

    protected override IAdvisoryScanScheduleRepository CreateRepository()
    {
        return new DapperAdvisoryScanScheduleRepository(new TestSqlConnectionFactory(fixture.ConnectionString));
    }
}
