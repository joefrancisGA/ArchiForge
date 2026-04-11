using ArchLucid.Contracts.Common;
using ArchLucid.Contracts.Metadata;
using ArchLucid.Persistence.Data.Repositories;

using FluentAssertions;

#pragma warning disable CS0618 // RunsAuthorityConvergence: tracked for migration by 2026-09-30 — contract tests for legacy IArchitectureRunRepository implementations.

namespace ArchLucid.Persistence.Tests.Contracts;

/// <summary>
/// Shared contract assertions for <see cref="IArchitectureRunRepository"/>.
/// </summary>
public abstract class ArchitectureRunRepositoryContractTests
{
    protected virtual void SkipIfSqlServerUnavailable()
    {
    }

    protected abstract IArchitectureRunRepository CreateRepository();

    /// <summary>SQL: ensures FK to <c>ArchitectureRequests</c>; in-memory: seeds companion request repo.</summary>
    protected virtual Task PrepareRequestRowAsync(string requestId, string systemName, CancellationToken ct)
    {
        _ = requestId;
        _ = systemName;
        _ = ct;

        return Task.CompletedTask;
    }

    [SkippableFact]
    public async Task Create_then_GetById_round_trips()
    {
        SkipIfSqlServerUnavailable();
        IArchitectureRunRepository repo = CreateRepository();
        string requestId = "arun-req-" + Guid.NewGuid().ToString("N");
        string runId = Guid.NewGuid().ToString("N");

        await PrepareRequestRowAsync(requestId, "RunSys", CancellationToken.None);

        ArchitectureRun run = NewRun(runId, requestId, ArchitectureRunStatus.TasksGenerated);

        await repo.CreateAsync(run, CancellationToken.None);

        ArchitectureRun? loaded = await repo.GetByIdAsync(runId, CancellationToken.None);

        loaded.Should().NotBeNull();
        loaded.RunId.Should().Be(runId);
        loaded.Status.Should().Be(ArchitectureRunStatus.TasksGenerated);
    }

    [SkippableFact]
    public async Task UpdateStatusAsync_with_expectedStatus_transitions()
    {
        SkipIfSqlServerUnavailable();
        IArchitectureRunRepository repo = CreateRepository();
        string requestId = "arun2-req-" + Guid.NewGuid().ToString("N");
        string runId = Guid.NewGuid().ToString("N");

        await PrepareRequestRowAsync(requestId, "S", CancellationToken.None);

        await repo.CreateAsync(NewRun(runId, requestId, ArchitectureRunStatus.TasksGenerated), CancellationToken.None);

        await repo.UpdateStatusAsync(
            runId,
            ArchitectureRunStatus.ReadyForCommit,
            currentManifestVersion: null,
            completedUtc: null,
            cancellationToken: CancellationToken.None,
            expectedStatus: ArchitectureRunStatus.TasksGenerated);

        ArchitectureRun? loaded = await repo.GetByIdAsync(runId, CancellationToken.None);

        loaded.Should().NotBeNull();
        loaded.Status.Should().Be(ArchitectureRunStatus.ReadyForCommit);
    }

    [SkippableFact]
    public async Task UpdateStatusAsync_when_expectedStatus_mismatch_throws()
    {
        SkipIfSqlServerUnavailable();
        IArchitectureRunRepository repo = CreateRepository();
        string requestId = "arun3-req-" + Guid.NewGuid().ToString("N");
        string runId = Guid.NewGuid().ToString("N");

        await PrepareRequestRowAsync(requestId, "S", CancellationToken.None);

        await repo.CreateAsync(NewRun(runId, requestId, ArchitectureRunStatus.Created), CancellationToken.None);

        Func<Task> act = async () => await repo.UpdateStatusAsync(
            runId,
            ArchitectureRunStatus.ReadyForCommit,
            cancellationToken: CancellationToken.None,
            expectedStatus: ArchitectureRunStatus.TasksGenerated);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [SkippableFact]
    public async Task ListAsync_includes_system_name_from_request()
    {
        SkipIfSqlServerUnavailable();
        IArchitectureRunRepository repo = CreateRepository();
        string requestId = "arun4-req-" + Guid.NewGuid().ToString("N");
        string runId = Guid.NewGuid().ToString("N");

        await PrepareRequestRowAsync(requestId, "ListedSystem", CancellationToken.None);

        await repo.CreateAsync(NewRun(runId, requestId, ArchitectureRunStatus.Created), CancellationToken.None);

        IReadOnlyList<ArchitectureRunListItem> list = await repo.ListAsync(CancellationToken.None);

        ArchitectureRunListItem? row = list.FirstOrDefault(x => x.RunId == runId);
        row.Should().NotBeNull();
        row.SystemName.Should().Be("ListedSystem");
    }

    private static ArchitectureRun NewRun(string runId, string requestId, ArchitectureRunStatus status)
    {
        return new ArchitectureRun
        {
            RunId = runId,
            RequestId = requestId,
            Status = status,
            CreatedUtc = DateTime.UtcNow,
        };
    }
}

#pragma warning restore CS0618
