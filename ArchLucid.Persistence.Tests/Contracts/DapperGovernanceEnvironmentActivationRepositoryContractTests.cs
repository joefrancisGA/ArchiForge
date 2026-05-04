using ArchLucid.Persistence.Data.Repositories;

namespace ArchLucid.Persistence.Tests.Contracts;

[Collection(nameof(SqlServerPersistenceCollection))]
[Trait("Category", "SqlServerContainer")]
public sealed class DapperGovernanceEnvironmentActivationRepositoryContractTests(SqlServerPersistenceFixture fixture)
    : GovernanceEnvironmentActivationRepositoryContractTests
{
    protected override void SkipIfSqlServerUnavailable()
    {
        Skip.IfNot(fixture.IsSqlServerAvailable, SqlServerPersistenceFixture.SqlServerUnavailableSkipReason);
    }

    protected override IGovernanceEnvironmentActivationRepository CreateRepository()
    {
        return new TenantPrimingGovernanceEnvironmentActivationRepository(
            fixture.ConnectionString,
            new FixedTestScopeContextProvider(GovernanceRepositoryContractScope.AsScopeContext()));
    }
}
