using ArchLucid.Contracts.Metadata;
using ArchLucid.Persistence.Data.Repositories;

namespace ArchLucid.Persistence.Tests.Data.Repositories;

public sealed class InMemoryComparisonRecordRepositoryAdditionalCoverageTests
{
    private static ComparisonRecord Row(
        string id,
        DateTime created,
        string leftRun = "L",
        string rightRun = "R",
        string? label = null,
        List<string>? tags = null)
    {
        return new ComparisonRecord
        {
            ComparisonRecordId = id,
            ComparisonType = "t",
            LeftRunId = leftRun,
            RightRunId = rightRun,
            Format = "json",
            SummaryMarkdown = "s",
            PayloadJson = "{}",
            CreatedUtc = created,
            Label = label,
            Tags = tags ?? []
        };
    }

    [SkippableFact]
    public async Task SearchAsync_limit_zero_uses_default_page_size()
    {
        InMemoryComparisonRecordRepository sut = new();
        await sut.CreateAsync(Row("a", DateTime.UtcNow), CancellationToken.None);

        IReadOnlyList<ComparisonRecord> page =
            await sut.SearchAsync(null, null, null, null, null, null, null, null, null, null, null, 0, 0,
                CancellationToken.None);

        page.Should().ContainSingle();
    }

    [SkippableFact]
    public async Task SearchAsync_sorts_by_label_ascending()
    {
        InMemoryComparisonRecordRepository sut = new();
        DateTime t = DateTime.UtcNow;
        await sut.CreateAsync(Row("x", t, label: "b"), CancellationToken.None);
        await sut.CreateAsync(Row("y", t, label: "a"), CancellationToken.None);

        IReadOnlyList<ComparisonRecord> page = await sut.SearchAsync(
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
            "asc",
            0,
            10,
            CancellationToken.None);

        page.Select(p => p.Label).Should().ContainInOrder("a", "b");
    }

    [SkippableFact]
    public async Task SearchByCursor_ascending_pages_after_cursor()
    {
        InMemoryComparisonRecordRepository sut = new();
        DateTime t0 = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        await sut.CreateAsync(Row("early", t0), CancellationToken.None);
        await sut.CreateAsync(Row("late", t0.AddMinutes(1)), CancellationToken.None);

        IReadOnlyList<ComparisonRecord> page = await sut.SearchByCursorAsync(
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            "createdUtc",
            "asc",
            t0,
            "early",
            10,
            CancellationToken.None);

        page.Should().ContainSingle(r => r.ComparisonRecordId == "late");
    }

    [SkippableFact]
    public async Task GetByExportRecordIdAsync_matches_left_or_right()
    {
        InMemoryComparisonRecordRepository sut = new();
        ComparisonRecord r = Row("c", DateTime.UtcNow);
        r.LeftExportRecordId = "exp-left";
        await sut.CreateAsync(r, CancellationToken.None);

        IReadOnlyList<ComparisonRecord> list =
            await sut.GetByExportRecordIdAsync("exp-left", CancellationToken.None);

        list.Should().ContainSingle(x => x.ComparisonRecordId == "c");
    }

    [SkippableFact]
    public async Task UpdateLabelAndTags_unknown_id_returns_false()
    {
        InMemoryComparisonRecordRepository sut = new();

        bool ok = await sut.UpdateLabelAndTagsAsync("missing", "l", [], CancellationToken.None);

        ok.Should().BeFalse();
    }

    [SkippableFact]
    public async Task CreateAsync_throws_when_cancelled()
    {
        InMemoryComparisonRecordRepository sut = new();
        CancellationTokenSource cts = new();
        await cts.CancelAsync();

        Func<Task> act = async () =>
            await sut.CreateAsync(Row("z", DateTime.UtcNow), cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [SkippableFact]
    public async Task ReplacePayloadJsonForIntegrationTest_updates_payload()
    {
        InMemoryComparisonRecordRepository sut = new();
        ComparisonRecord r = Row("p", DateTime.UtcNow);
        await sut.CreateAsync(r, CancellationToken.None);

        sut.ReplacePayloadJsonForIntegrationTest("p", "{\"k\":1}");

        ComparisonRecord? loaded = await sut.GetByIdAsync("p", CancellationToken.None);

        loaded!.PayloadJson.Should().Be("{\"k\":1}");
    }

    [SkippableFact]
    public void ReplacePayloadJsonForIntegrationTest_missing_throws()
    {
        InMemoryComparisonRecordRepository sut = new();

        Action act = () => sut.ReplacePayloadJsonForIntegrationTest("nope", "{}");

        act.Should().Throw<InvalidOperationException>();
    }
}
