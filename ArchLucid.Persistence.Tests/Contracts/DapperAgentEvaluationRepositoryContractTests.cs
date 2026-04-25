using ArchLucid.Contracts.Agents;
using ArchLucid.Contracts.Common;
using ArchLucid.Persistence.Data.Repositories;
using ArchLucid.Persistence.Tests.Support;

using Microsoft.Data.SqlClient;

namespace ArchLucid.Persistence.Tests.Contracts;

[Collection(nameof(SqlServerPersistenceCollection))]
[Trait("Category", "SqlServerContainer")]
public sealed class DapperAgentEvaluationRepositoryContractTests(SqlServerPersistenceFixture fixture)
    : AgentEvaluationRepositoryContractTests
{
    protected override void SkipIfSqlServerUnavailable()
    {
        Skip.IfNot(fixture.IsSqlServerAvailable, SqlServerPersistenceFixture.SqlServerUnavailableSkipReason);
    }

    protected override IAgentEvaluationRepository CreateRepository()
    {
        return new AgentEvaluationRepository(new TestSqlDbConnectionFactory(fixture.ConnectionString));
    }

    protected override async Task PrepareRunAndTaskAsync(string requestId, string runId, string taskId,
        CancellationToken ct)
    {
        await using SqlConnection connection = new(fixture.ConnectionString);
        await connection.OpenAsync(ct);
        await ArchitectureCommitTestSeed.InsertRequestAndRunAsync(connection, requestId, runId, ct);

        AgentTask task = new()
        {
            TaskId = taskId,
            RunId = runId,
            AgentType = AgentType.Topology,
            Objective = "seed",
            Status = AgentTaskStatus.Created,
            CreatedUtc = DateTime.UtcNow,
            EvidenceBundleRef = "eb-seed"
        };

        await ArchitectureCommitTestSeed.InsertAgentTaskAsync(connection, task, ct);
    }
}
