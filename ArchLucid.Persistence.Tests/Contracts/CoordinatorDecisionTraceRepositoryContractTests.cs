using ArchLucid.Contracts.DecisionTraces;
using ArchLucid.Persistence.Data.Repositories;

using FluentAssertions;

namespace ArchLucid.Persistence.Tests.Contracts;

/// <summary>
/// Shared contract assertions for coordinator <see cref="ICoordinatorDecisionTraceRepository"/> (run-scoped decision log rows).
/// </summary>
public abstract class CoordinatorDecisionTraceRepositoryContractTests
{
    protected virtual void SkipIfSqlServerUnavailable()
    {
    }

    protected abstract ICoordinatorDecisionTraceRepository CreateRepository();

    /// <summary>SQL: ensures <c>dbo.Runs</c> + <c>dbo.ArchitectureRequests</c> exist for <paramref name="runId"/>.</summary>
    protected virtual Task PrepareRunForCoordinatorDataAsync(string requestId, string runId, CancellationToken ct)
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
        ICoordinatorDecisionTraceRepository repo = CreateRepository();
        string runId = Guid.NewGuid().ToString("N");
        string requestId = "req-dt-" + Guid.NewGuid().ToString("N");
        await PrepareRunForCoordinatorDataAsync(requestId, runId, CancellationToken.None);
        DateTime t0 = new(2026, 4, 1, 10, 0, 0, DateTimeKind.Utc);
        DateTime t1 = new(2026, 4, 1, 11, 0, 0, DateTimeKind.Utc);

        List<DecisionTrace> batch =
        [
            RunEventTrace.From(new RunEventTracePayload
            {
                TraceId = "a-" + Guid.NewGuid().ToString("N"),
                RunId = runId,
                EventType = "second",
                EventDescription = "d2",
                CreatedUtc = t1,
            }),
            RunEventTrace.From(new RunEventTracePayload
            {
                TraceId = "b-" + Guid.NewGuid().ToString("N"),
                RunId = runId,
                EventType = "first",
                EventDescription = "d1",
                CreatedUtc = t0,
            }),
        ];

        await repo.CreateManyAsync(batch, CancellationToken.None);

        IReadOnlyList<DecisionTrace> loaded = await repo.GetByRunIdAsync(runId, CancellationToken.None);

        loaded.Should().HaveCount(2);
        loaded[0].RequireRunEvent().EventType.Should().Be("first");
        loaded[1].RequireRunEvent().EventType.Should().Be("second");
    }

    [SkippableFact]
    public async Task GetByRunId_empty_run_returns_empty_list()
    {
        SkipIfSqlServerUnavailable();
        ICoordinatorDecisionTraceRepository repo = CreateRepository();

        IReadOnlyList<DecisionTrace> loaded =
            await repo.GetByRunIdAsync("no-such-" + Guid.NewGuid().ToString("N"), CancellationToken.None);

        loaded.Should().BeEmpty();
    }
}
