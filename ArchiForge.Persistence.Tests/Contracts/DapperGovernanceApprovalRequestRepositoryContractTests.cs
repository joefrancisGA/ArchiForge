using ArchiForge.Persistence.Data.Repositories;

namespace ArchiForge.Persistence.Tests.Contracts;

[Collection(nameof(SqlServerPersistenceCollection))]
[Trait("Category", "SqlServerContainer")]
public sealed class DapperGovernanceApprovalRequestRepositoryContractTests(SqlServerPersistenceFixture fixture)
    : GovernanceApprovalRequestRepositoryContractTests
{
    protected override void SkipIfSqlServerUnavailable()
    {
        Skip.IfNot(fixture.IsSqlServerAvailable, SqlServerPersistenceFixture.SqlServerUnavailableSkipReason);
    }

    protected override IGovernanceApprovalRequestRepository CreateRepository()
    {
        return new GovernanceApprovalRequestRepository(new TestSqlDbConnectionFactory(fixture.ConnectionString));
    }
}
