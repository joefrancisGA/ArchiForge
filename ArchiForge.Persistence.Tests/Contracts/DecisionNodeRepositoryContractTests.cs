using ArchiForge.Contracts.Decisions;
using ArchiForge.Persistence.Data.Repositories;

using FluentAssertions;

namespace ArchiForge.Persistence.Tests.Contracts;

/// <summary>
/// Shared contract assertions for <see cref="IDecisionNodeRepository"/>.
/// </summary>
public abstract class DecisionNodeRepositoryContractTests
{
    protected virtual void SkipIfSqlServerUnavailable()
    {
    }

    protected abstract IDecisionNodeRepository CreateRepository();

    protected virtual Task PrepareRunAsync(string requestId, string runId, CancellationToken ct)
    {
        _ = requestId;
        _ = runId;
        _ = ct;

        return Task.CompletedTask;
    }

    [SkippableFact]
    public async Task Two_CreateMany_batches_append_for_same_run()
    {
        SkipIfSqlServerUnavailable();
        IDecisionNodeRepository repo = CreateRepository();
        string runId = Guid.NewGuid().ToString("N");
        string requestId = "req-dn-" + Guid.NewGuid().ToString("N");
        await PrepareRunAsync(requestId, runId, CancellationToken.None);
        DateTime t0 = new(2026, 4, 1, 8, 0, 0, DateTimeKind.Utc);
        DateTime t1 = new(2026, 4, 1, 9, 0, 0, DateTimeKind.Utc);

        await repo.CreateManyAsync(
            [NewNode(runId, "d1", "topic-a", t0)],
            CancellationToken.None);

        await repo.CreateManyAsync(
            [NewNode(runId, "d2", "topic-b", t1)],
            CancellationToken.None);

        IReadOnlyList<DecisionNode> loaded = await repo.GetByRunIdAsync(runId, CancellationToken.None);

        loaded.Should().HaveCount(2);
        loaded[0].DecisionId.Should().Be("d1");
        loaded[1].DecisionId.Should().Be("d2");
    }

    private static DecisionNode NewNode(string runId, string decisionId, string topic, DateTime createdUtc)
    {
        return new DecisionNode
        {
            DecisionId = decisionId,
            RunId = runId,
            Topic = topic,
            Rationale = "r",
            Confidence = 0.5,
            CreatedUtc = createdUtc,
        };
    }
}
