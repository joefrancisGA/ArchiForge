using ArchLucid.Persistence.Data.Repositories;
using ArchLucid.Persistence.Tests.Support;

using Microsoft.Data.SqlClient;

namespace ArchLucid.Persistence.Tests.Contracts;

[Collection(nameof(SqlServerPersistenceCollection))]
[Trait("Category", "SqlServerContainer")]
public sealed class DapperDecisionNodeRepositoryContractTests(SqlServerPersistenceFixture fixture)
    : DecisionNodeRepositoryContractTests
{
    protected override void SkipIfSqlServerUnavailable()
    {
        Assert.SkipUnless(fixture.IsSqlServerAvailable, SqlServerPersistenceFixture.SqlServerUnavailableSkipReason);
    }

    protected override IDecisionNodeRepository CreateRepository()
    {
        return new DecisionNodeRepository(new TestSqlDbConnectionFactory(fixture.ConnectionString));
    }

    protected override async Task PrepareRunAsync(string requestId, string runId, CancellationToken ct)
    {
        await using SqlConnection connection = new(fixture.ConnectionString);
        await connection.OpenAsync(ct);
        await ArchitectureCommitTestSeed.InsertRequestAndRunAsync(connection, requestId, runId, ct);
    }
}
