using ArchiForge.Contracts.Agents;
using ArchiForge.Contracts.Common;

using ArchiForge.Persistence.Data.Repositories;

using ArchiForge.Persistence.Tests.Support;

using Microsoft.Data.SqlClient;

namespace ArchiForge.Persistence.Tests.Contracts;

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

    protected override async Task PrepareRunAndTaskAsync(string requestId, string runId, string taskId, CancellationToken ct)
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
            EvidenceBundleRef = "eb-seed",
        };

        await ArchitectureCommitTestSeed.InsertAgentTaskAsync(connection, task, ct);
    }
}
