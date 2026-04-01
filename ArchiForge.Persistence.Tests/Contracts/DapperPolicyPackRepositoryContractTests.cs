using ArchiForge.Decisioning.Governance.PolicyPacks;

using ArchiForge.Persistence.Governance;

namespace ArchiForge.Persistence.Tests.Contracts;

/// <summary>
/// Runs <see cref="PolicyPackRepositoryContractTests"/> against <see cref="DapperPolicyPackRepository"/>.
/// </summary>
[Collection(nameof(SqlServerPersistenceCollection))]
[Trait("Category", "SqlServerContainer")]
public sealed class DapperPolicyPackRepositoryContractTests(SqlServerPersistenceFixture fixture)
    : PolicyPackRepositoryContractTests
{
    protected override void SkipIfSqlServerUnavailable()
    {
        Skip.IfNot(fixture.IsSqlServerAvailable, SqlServerPersistenceFixture.SqlServerUnavailableSkipReason);
    }

    protected override IPolicyPackRepository CreateRepository()
    {
        return new DapperPolicyPackRepository(new TestSqlConnectionFactory(fixture.ConnectionString));
    }
}
