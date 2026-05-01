using ArchLucid.Core.Scoping;
using ArchLucid.Decisioning.Interfaces;
using ArchLucid.Persistence.Connections;
using ArchLucid.Persistence.Repositories;
using ArchLucid.Persistence.Tests.Support;

using Microsoft.Data.SqlClient;

namespace ArchLucid.Persistence.Tests.Contracts;

/// <summary>
///     Runs <see cref="DecisionTraceRepositoryContractTests" /> against <see cref="SqlDecisionTraceRepository" />.
/// </summary>
[Collection(nameof(SqlServerPersistenceCollection))]
[Trait("Category", "SqlServerContainer")]
public sealed class SqlDecisionTraceRepositoryContractTests(SqlServerPersistenceFixture fixture)
    : DecisionTraceRepositoryContractTests
{
    protected override void SkipIfSqlServerUnavailable()
    {
        Assert.SkipUnless(fixture.IsSqlServerAvailable, SqlServerPersistenceFixture.SqlServerUnavailableSkipReason);
    }

    protected override IDecisionTraceRepository CreateRepository()
    {
        return new SqlDecisionTraceRepository(new TestSqlConnectionFactory(fixture.ConnectionString));
    }

    protected override async Task PrepareRunForTraceAsync(ScopeContext scope, Guid runId, CancellationToken ct)
    {
        SqlConnectionFactory factory = new(fixture.ConnectionString);
        await using SqlConnection connection = await factory.CreateOpenConnectionAsync(ct);
        await AuthorityRunChainTestSeed.InsertRunAsync(
            connection,
            scope.TenantId,
            scope.WorkspaceId,
            scope.ProjectId,
            runId,
            "contract-proj-dt",
            ct);
    }
}
