using ArchLucid.Persistence.Data.Repositories;

namespace ArchLucid.Persistence.Tests.Contracts;

[Collection(nameof(SqlServerPersistenceCollection))]
[Trait("Category", "SqlServerContainer")]
public sealed class DapperGovernanceApprovalRequestRepositoryContractTests(SqlServerPersistenceFixture fixture)
    : GovernanceApprovalRequestRepositoryContractTests
{
    protected override void SkipIfSqlServerUnavailable()
    {
        Assert.SkipUnless(fixture.IsSqlServerAvailable, SqlServerPersistenceFixture.SqlServerUnavailableSkipReason);
    }

    protected override IGovernanceApprovalRequestRepository CreateRepository()
    {
        return new GovernanceApprovalRequestRepository(
            new TestSqlDbConnectionFactory(fixture.ConnectionString),
            new FixedTestScopeContextProvider(GovernanceRepositoryContractScope.AsScopeContext()));
    }
}
