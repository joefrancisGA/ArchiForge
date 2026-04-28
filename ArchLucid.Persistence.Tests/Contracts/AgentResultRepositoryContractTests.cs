using ArchLucid.Contracts.Agents;
using ArchLucid.Contracts.Common;
using ArchLucid.Persistence.Data.Repositories;

namespace ArchLucid.Persistence.Tests.Contracts;

/// <summary>
///     Shared contract assertions for <see cref="IAgentResultRepository" />.
/// </summary>
public abstract class AgentResultRepositoryContractTests
{
    protected virtual void SkipIfSqlServerUnavailable()
    {
    }

    protected abstract IAgentResultRepository CreateRepository();

    protected virtual Task PrepareRunTaskChainAsync(string requestId, string runId, AgentTask task,
        CancellationToken ct)
    {
        _ = requestId;
        _ = runId;
        _ = task;
        _ = ct;

        return Task.CompletedTask;
    }

    [SkippableFact]
    public async Task Create_then_GetByRunId_returns_result()
    {
        SkipIfSqlServerUnavailable();
        IAgentResultRepository repo = CreateRepository();
        string requestId = "arr-req-" + Guid.NewGuid().ToString("N");
        string runId = Guid.NewGuid().ToString("N");
        AgentTask task = NewTaskRow(runId, "res-task-1");

        await PrepareRunTaskChainAsync(requestId, runId, task, CancellationToken.None);

        AgentResult result = NewResult(runId, task.TaskId, "r1", DateTime.UtcNow);

        await repo.CreateAsync(result, CancellationToken.None);

        IReadOnlyList<AgentResult> loaded = await repo.GetByRunIdAsync(runId, CancellationToken.None);

        loaded.Should().ContainSingle();
        loaded[0].TaskId.Should().Be(task.TaskId);
        loaded[0].ResultId.Should().Be("r1");
    }

    [SkippableFact]
    public async Task CreateMany_replaces_prior_results_for_same_run()
    {
        SkipIfSqlServerUnavailable();
        IAgentResultRepository repo = CreateRepository();
        string requestId = "arr2-req-" + Guid.NewGuid().ToString("N");
        string runId = Guid.NewGuid().ToString("N");
        AgentTask task = NewTaskRow(runId, "res-task-2");

        await PrepareRunTaskChainAsync(requestId, runId, task, CancellationToken.None);

        await repo.CreateAsync(NewResult(runId, task.TaskId, "old", DateTime.UtcNow.AddMinutes(-1)),
            CancellationToken.None);

        await repo.CreateManyAsync(
            [NewResult(runId, task.TaskId, "new", DateTime.UtcNow)],
            CancellationToken.None);

        IReadOnlyList<AgentResult> loaded = await repo.GetByRunIdAsync(runId, CancellationToken.None);

        loaded.Should().ContainSingle();
        loaded[0].ResultId.Should().Be("new");
    }

    private static AgentTask NewTaskRow(string runId, string taskId)
    {
        return new AgentTask
        {
            TaskId = taskId,
            RunId = runId,
            AgentType = AgentType.Topology,
            Objective = "o",
            Status = AgentTaskStatus.Created,
            CreatedUtc = DateTime.UtcNow,
            EvidenceBundleRef = "eb-ar"
        };
    }

    private static AgentResult NewResult(string runId, string taskId, string resultId, DateTime createdUtc)
    {
        return new AgentResult
        {
            ResultId = resultId,
            TaskId = taskId,
            RunId = runId,
            AgentType = AgentType.Topology,
            Claims = [],
            EvidenceRefs = [],
            Confidence = 0.5,
            CreatedUtc = createdUtc
        };
    }
}
