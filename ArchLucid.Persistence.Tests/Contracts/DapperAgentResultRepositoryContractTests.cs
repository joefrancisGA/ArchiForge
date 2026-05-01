using ArchLucid.Contracts.Agents;
using ArchLucid.Persistence.Data.Repositories;
using ArchLucid.Persistence.Tests.Support;

using Microsoft.Data.SqlClient;

namespace ArchLucid.Persistence.Tests.Contracts;

[Collection(nameof(SqlServerPersistenceCollection))]
[Trait("Category", "SqlServerContainer")]
public sealed class DapperAgentResultRepositoryContractTests(SqlServerPersistenceFixture fixture)
    : AgentResultRepositoryContractTests
{
    protected override void SkipIfSqlServerUnavailable()
    {
        Assert.SkipUnless(fixture.IsSqlServerAvailable, SqlServerPersistenceFixture.SqlServerUnavailableSkipReason);
    }

    protected override IAgentResultRepository CreateRepository()
    {
        return new AgentResultRepository(new TestSqlDbConnectionFactory(fixture.ConnectionString));
    }

    protected override async Task PrepareRunTaskChainAsync(string requestId, string runId, AgentTask task,
        CancellationToken ct)
    {
        await using SqlConnection connection = new(fixture.ConnectionString);
        await connection.OpenAsync(ct);
        await ArchitectureCommitTestSeed.InsertRequestAndRunAsync(connection, requestId, runId, ct);
        await ArchitectureCommitTestSeed.InsertAgentTaskAsync(connection, task, ct);
    }
}
