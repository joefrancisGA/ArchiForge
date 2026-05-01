using ArchLucid.Contracts.Metadata;
using ArchLucid.Persistence.Data.Repositories;

namespace ArchLucid.Persistence.Tests.Contracts;

/// <summary>
///     Shared contract assertions for <see cref="IRunExportRecordRepository" />.
/// </summary>
public abstract class RunExportRecordRepositoryContractTests
{
    protected virtual void SkipIfSqlServerUnavailable()
    {
    }

    protected abstract IRunExportRecordRepository CreateRepository();

    protected virtual Task PrepareRunAsync(string requestId, string runId, CancellationToken ct)
    {
        _ = requestId;
        _ = runId;
        _ = ct;

        return Task.CompletedTask;
    }

    [Fact]
    public async Task Create_then_GetById_round_trips()
    {
        SkipIfSqlServerUnavailable();
        IRunExportRecordRepository repo = CreateRepository();
        string runId = Guid.NewGuid().ToString("N");
        string requestId = "req-exp-" + Guid.NewGuid().ToString("N");
        await PrepareRunAsync(requestId, runId, CancellationToken.None);
        string exportId = "exp-" + Guid.NewGuid().ToString("N");

        RunExportRecord record = NewRecord(exportId, runId, new DateTime(2026, 4, 1, 12, 0, 0, DateTimeKind.Utc));

        await repo.CreateAsync(record, CancellationToken.None);

        RunExportRecord? loaded = await repo.GetByIdAsync(exportId, CancellationToken.None);

        loaded.Should().NotBeNull();
        loaded.ExportRecordId.Should().Be(exportId);
        loaded.RunId.Should().Be(runId);
    }

    [Fact]
    public async Task GetByRunId_orders_descending_by_CreatedUtc()
    {
        SkipIfSqlServerUnavailable();
        IRunExportRecordRepository repo = CreateRepository();
        string runId = Guid.NewGuid().ToString("N");
        string requestId = "req-exp2-" + Guid.NewGuid().ToString("N");
        await PrepareRunAsync(requestId, runId, CancellationToken.None);
        DateTime older = new(2026, 4, 1, 10, 0, 0, DateTimeKind.Utc);
        DateTime newer = new(2026, 4, 1, 11, 0, 0, DateTimeKind.Utc);

        await repo.CreateAsync(NewRecord("e-old", runId, older), CancellationToken.None);
        await repo.CreateAsync(NewRecord("e-new", runId, newer), CancellationToken.None);

        IReadOnlyList<RunExportRecord> list = await repo.GetByRunIdAsync(runId, CancellationToken.None);

        list.Should().HaveCount(2);
        list[0].ExportRecordId.Should().Be("e-new");
        list[1].ExportRecordId.Should().Be("e-old");
    }

    private static RunExportRecord NewRecord(string exportId, string runId, DateTime createdUtc)
    {
        return new RunExportRecord
        {
            ExportRecordId = exportId,
            RunId = runId,
            ExportType = "analysis",
            Format = "markdown",
            FileName = "out.md",
            CreatedUtc = createdUtc
        };
    }
}
