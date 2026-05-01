using ArchLucid.Contracts.Agents;
using ArchLucid.Persistence.Data.Repositories;
using ArchLucid.Persistence.Tests.Support;

using Microsoft.Data.SqlClient;

namespace ArchLucid.Persistence.Tests.Contracts;

[Collection(nameof(SqlServerPersistenceCollection))]
[Trait("Category", "SqlServerContainer")]
public sealed class DapperAgentExecutionTraceRepositoryContractTests(SqlServerPersistenceFixture fixture)
    : AgentExecutionTraceRepositoryContractTests
{
    protected override void SkipIfSqlServerUnavailable()
    {
        Skip.IfNot(fixture.IsSqlServerAvailable, SqlServerPersistenceFixture.SqlServerUnavailableSkipReason);
    }

    protected override IAgentExecutionTraceRepository CreateRepository()
    {
        return new AgentExecutionTraceRepository(new TestSqlDbConnectionFactory(fixture.ConnectionString));
    }

    protected override async Task PrepareRunAndTaskAsync(string requestId, string runId, AgentTask task,
        CancellationToken ct)
    {
        await using SqlConnection connection = new(fixture.ConnectionString);
        await connection.OpenAsync(ct);
        await ArchitectureCommitTestSeed.InsertRequestAndRunAsync(connection, requestId, runId, ct);
        await ArchitectureCommitTestSeed.InsertAgentTaskAsync(connection, task, ct);
    }
}
