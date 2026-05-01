using ArchLucid.Decisioning.Governance.PolicyPacks;
using ArchLucid.Persistence.Connections;
using ArchLucid.Persistence.Governance;

namespace ArchLucid.Persistence.Tests.Contracts;

/// <summary>
///     Runs <see cref="PolicyPackRepositoryContractTests" /> against <see cref="DapperPolicyPackRepository" />.
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
        TestSqlConnectionFactory sql = new(fixture.ConnectionString);
        SqlPrimaryMirroredReadReplicaConnectionFactory readMirror = new(sql);
        return new DapperPolicyPackRepository(sql, readMirror);
    }
}
