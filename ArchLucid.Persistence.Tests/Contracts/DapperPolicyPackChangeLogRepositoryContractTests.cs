using ArchLucid.Decisioning.Governance.PolicyPacks;
using ArchLucid.Persistence.Connections;
using ArchLucid.Persistence.Governance;

namespace ArchLucid.Persistence.Tests.Contracts;

/// <summary>
///     Runs <see cref="PolicyPackChangeLogRepositoryContractTests" /> against
///     <see cref="DapperPolicyPackChangeLogRepository" />.
/// </summary>
[Collection(nameof(SqlServerPersistenceCollection))]
[Trait("Category", "SqlServerContainer")]
public sealed class DapperPolicyPackChangeLogRepositoryContractTests(SqlServerPersistenceFixture fixture)
    : PolicyPackChangeLogRepositoryContractTests
{
    protected override void SkipIfSqlServerUnavailable()
    {
        Skip.IfNot(fixture.IsSqlServerAvailable, SqlServerPersistenceFixture.SqlServerUnavailableSkipReason);
    }

    protected override IPolicyPackChangeLogRepository CreateRepository()
    {
        SqlConnectionFactory sql = new(fixture.ConnectionString);
        SqlPrimaryMirroredReadReplicaConnectionFactory readMirror = new(sql);

        return new DapperPolicyPackChangeLogRepository(sql, readMirror);
    }
}
