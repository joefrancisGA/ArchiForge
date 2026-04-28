using ArchLucid.Contracts.Metadata;
using ArchLucid.Persistence.Data.Repositories;

namespace ArchLucid.Persistence.Tests.Contracts;

/// <summary>
///     Shared contract assertions for <see cref="IComparisonRecordRepository" />.
/// </summary>
public abstract class ComparisonRecordRepositoryContractTests
{
    protected abstract IComparisonRecordRepository CreateRepository();

    /// <summary>No-op for in-memory; Dapper + SQL subclasses skip when SQL is unavailable.</summary>
    protected virtual void SkipIfSqlServerUnavailable()
    {
    }

    private static ComparisonRecord CreateRecord(string id, string comparisonType = "end-to-end-replay")
    {
        return new ComparisonRecord
        {
            ComparisonRecordId = id,
            ComparisonType = comparisonType,
            LeftRunId = "L1",
            RightRunId = "R1",
            Format = "json+markdown",
            SummaryMarkdown = "s",
            PayloadJson = "{}",
            CreatedUtc = DateTime.UtcNow,
            Label = "lbl",
            Tags = ["a", "b"]
        };
    }

    [SkippableFact]
    public async Task Create_then_GetById_returns_same_record()
    {
        SkipIfSqlServerUnavailable();
        IComparisonRecordRepository repo = CreateRepository();
        ComparisonRecord row = CreateRecord("cmp_contract_" + Guid.NewGuid().ToString("N"));

        await repo.CreateAsync(row, CancellationToken.None);

        ComparisonRecord? loaded = await repo.GetByIdAsync(row.ComparisonRecordId, CancellationToken.None);

        loaded.Should().NotBeNull();
        loaded.ComparisonRecordId.Should().Be(row.ComparisonRecordId);
        loaded.ComparisonType.Should().Be(row.ComparisonType);
        loaded.LeftRunId.Should().Be(row.LeftRunId);
        loaded.Tags.Should().BeEquivalentTo(row.Tags);
    }

    [SkippableFact]
    public async Task GetById_nonexistent_returns_null()
    {
        SkipIfSqlServerUnavailable();
        IComparisonRecordRepository repo = CreateRepository();

        ComparisonRecord? result =
            await repo.GetByIdAsync("missing_" + Guid.NewGuid().ToString("N"), CancellationToken.None);

        result.Should().BeNull();
    }

    [SkippableFact]
    public async Task GetByRunId_returns_matches()
    {
        SkipIfSqlServerUnavailable();
        IComparisonRecordRepository repo = CreateRepository();
        string runId = "run_scope_" + Guid.NewGuid().ToString("N");
        ComparisonRecord a = CreateRecord("cmp_a_" + Guid.NewGuid().ToString("N"));
        a.LeftRunId = runId;
        ComparisonRecord b = CreateRecord("cmp_b_" + Guid.NewGuid().ToString("N"));
        b.RightRunId = runId;

        await repo.CreateAsync(a, CancellationToken.None);
        await repo.CreateAsync(b, CancellationToken.None);

        IReadOnlyList<ComparisonRecord> list = await repo.GetByRunIdAsync(runId, CancellationToken.None);

        list.Count.Should().BeGreaterThanOrEqualTo(2);
        list.Select(x => x.ComparisonRecordId).Should().Contain(a.ComparisonRecordId, b.ComparisonRecordId);
    }

    [SkippableFact]
    public async Task UpdateLabelAndTags_persists()
    {
        SkipIfSqlServerUnavailable();
        IComparisonRecordRepository repo = CreateRepository();
        ComparisonRecord row = CreateRecord("cmp_upd_" + Guid.NewGuid().ToString("N"));
        await repo.CreateAsync(row, CancellationToken.None);

        bool ok = await repo.UpdateLabelAndTagsAsync(row.ComparisonRecordId, "new-label", ["x"],
            CancellationToken.None);

        ok.Should().BeTrue();

        ComparisonRecord? loaded = await repo.GetByIdAsync(row.ComparisonRecordId, CancellationToken.None);
        loaded!.Label.Should().Be("new-label");
        loaded.Tags.Should().BeEquivalentTo("x");
    }

    [SkippableFact]
    public async Task SearchAsync_filters_by_comparison_type()
    {
        SkipIfSqlServerUnavailable();
        IComparisonRecordRepository repo = CreateRepository();
        ComparisonRecord e2e = CreateRecord("cmp_e2e_" + Guid.NewGuid().ToString("N"));
        ComparisonRecord diff = CreateRecord("cmp_diff_" + Guid.NewGuid().ToString("N"), "export-record-diff");

        await repo.CreateAsync(e2e, CancellationToken.None);
        await repo.CreateAsync(diff, CancellationToken.None);

        IReadOnlyList<ComparisonRecord> list = await repo.SearchAsync(
            "export-record-diff",
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            "createdUtc",
            "desc",
            0,
            50,
            CancellationToken.None);

        list.Should().Contain(r => r.ComparisonRecordId == diff.ComparisonRecordId);
        list.Should().NotContain(r => r.ComparisonRecordId == e2e.ComparisonRecordId);
    }

    [SkippableFact]
    public async Task SearchByCursor_invalid_sort_throws()
    {
        SkipIfSqlServerUnavailable();
        IComparisonRecordRepository repo = CreateRepository();

        Func<Task> act = async () =>
            await repo.SearchByCursorAsync(
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                "label",
                "desc",
                null,
                null,
                10,
                CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }
}
