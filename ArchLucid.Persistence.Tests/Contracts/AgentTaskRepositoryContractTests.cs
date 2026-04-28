using ArchLucid.Contracts.Agents;
using ArchLucid.Contracts.Common;
using ArchLucid.Persistence.Data.Repositories;

namespace ArchLucid.Persistence.Tests.Contracts;

/// <summary>
///     Shared contract assertions for <see cref="IAgentTaskRepository" />.
/// </summary>
public abstract class AgentTaskRepositoryContractTests
{
    protected virtual void SkipIfSqlServerUnavailable()
    {
    }

    protected abstract IAgentTaskRepository CreateRepository();

    protected virtual Task PrepareRequestAndRunAsync(string requestId, string runId, CancellationToken ct)
    {
        _ = requestId;
        _ = runId;
        _ = ct;

        return Task.CompletedTask;
    }

    [SkippableFact]
    public async Task CreateMany_then_GetByRunId_orders_by_CreatedUtc()
    {
        SkipIfSqlServerUnavailable();
        IAgentTaskRepository repo = CreateRepository();
        string requestId = "atr-req-" + Guid.NewGuid().ToString("N");
        string runId = Guid.NewGuid().ToString("N");

        await PrepareRequestAndRunAsync(requestId, runId, CancellationToken.None);

        DateTime older = DateTime.UtcNow.AddMinutes(-5);
        DateTime newer = DateTime.UtcNow.AddMinutes(-1);

        List<AgentTask> tasks =
        [
            NewTask(runId, "t2", AgentType.Compliance, newer),
            NewTask(runId, "t1", AgentType.Topology, older)
        ];

        await repo.CreateManyAsync(tasks, CancellationToken.None);

        IReadOnlyList<AgentTask> loaded = await repo.GetByRunIdAsync(runId, CancellationToken.None);

        loaded.Should().HaveCount(2);
        loaded[0].TaskId.Should().Be("t1");
        loaded[1].TaskId.Should().Be("t2");
    }

    [SkippableFact]
    public async Task GetByRunId_when_none_returns_empty()
    {
        SkipIfSqlServerUnavailable();
        IAgentTaskRepository repo = CreateRepository();

        IReadOnlyList<AgentTask> loaded =
            await repo.GetByRunIdAsync(Guid.NewGuid().ToString("N"), CancellationToken.None);

        loaded.Should().BeEmpty();
    }

    private static AgentTask NewTask(string runId, string taskId, AgentType type, DateTime createdUtc)
    {
        return new AgentTask
        {
            TaskId = taskId,
            RunId = runId,
            AgentType = type,
            Objective = "obj",
            Status = AgentTaskStatus.Created,
            CreatedUtc = createdUtc,
            EvidenceBundleRef = "eb-contract"
        };
    }
}
