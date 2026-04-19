using System.Net;
using System.Net.Http.Json;

using ArchLucid.Contracts.Metadata;
using ArchLucid.Persistence.Data.Repositories;

using FluentAssertions;

using Microsoft.Extensions.DependencyInjection;

namespace ArchLucid.Api.Tests;

/// <summary>
/// Tests for Architecture Comparison Search.
/// </summary>

[Trait("Category", "Integration")]
public sealed class ArchitectureComparisonSearchTests(ArchLucidApiFactory factory) : IntegrationTestBase(factory)
{
    private readonly ArchLucidApiFactory _factory = factory;

    /// <summary>Records seeded so two pages of limit 2 do not overlap and ascending sort can be asserted.</summary>
    private const int SeededComparisonRecordsForPagingOverlapTest = 6;

    /// <summary>Records seeded for cursor-based paging (desc sort, limit 2 per page).</summary>
    private const int SeededComparisonRecordsForCursorPagingTest = 5;

    [Fact]
    public async Task SearchComparisons_PagingDoesNotOverlap_AndSortAscWorks()
    {
        List<string> ids = [];

        using (IServiceScope scope = _factory.Services.CreateScope())
        {
            IComparisonRecordRepository repo = scope.ServiceProvider.GetRequiredService<IComparisonRecordRepository>();
            for (int i = 0; i < SeededComparisonRecordsForPagingOverlapTest; i++)
            {
                ComparisonRecord record = new()
                {
                    ComparisonRecordId = $"cmp_search_{i}_{Guid.NewGuid():N}",
                    ComparisonType = "end-to-end-replay",
                    LeftRunId = "L",
                    RightRunId = "R",
                    Format = "json+markdown",
                    SummaryMarkdown = "s",
                    PayloadJson = "{}",
                    CreatedUtc = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddMinutes(i),
                    Label = i % 2 == 0 ? "even" : "odd",
                    Tags = i % 2 == 0 ? ["alpha", "beta"] : ["beta"]
                };
                await repo.CreateAsync(record);
                ids.Add(record.ComparisonRecordId);
            }
        }

        ComparisonHistoryResponseDto? page1 = await Client.GetFromJsonAsync<ComparisonHistoryResponseDto>(
            "/v1/architecture/comparisons?comparisonType=end-to-end-replay&limit=2&skip=0",
            JsonOptions);
        page1.Should().NotBeNull();
        page1.Records.Should().HaveCount(2);

        ComparisonHistoryResponseDto? page2 = await Client.GetFromJsonAsync<ComparisonHistoryResponseDto>(
            "/v1/architecture/comparisons?comparisonType=end-to-end-replay&limit=2&skip=2",
            JsonOptions);
        page2.Should().NotBeNull();
        page2.Records.Should().HaveCount(2);

        page1.Records.Select(r => r.ComparisonRecordId)
            .Intersect(page2.Records.Select(r => r.ComparisonRecordId))
            .Should().BeEmpty();

        // Ascending sort: first item should be the earliest CreatedUtc (minute 0)
        ComparisonHistoryResponseDto? asc = await Client.GetFromJsonAsync<ComparisonHistoryResponseDto>(
            $"/v1/architecture/comparisons?comparisonType=end-to-end-replay&limit={SeededComparisonRecordsForPagingOverlapTest}&skip=0&sortDir=asc",
            JsonOptions);
        asc.Should().NotBeNull();
        asc.Records.Should().HaveCount(SeededComparisonRecordsForPagingOverlapTest);
        asc.Records.First().ComparisonRecordId.Should().Be(ids[0]);
    }

    [Fact]
    public async Task SearchComparisons_CursorPaging_ReturnsNextPage()
    {
        using (IServiceScope scope = _factory.Services.CreateScope())
        {
            IComparisonRecordRepository repo = scope.ServiceProvider.GetRequiredService<IComparisonRecordRepository>();
            for (int i = 0; i < SeededComparisonRecordsForCursorPagingTest; i++)
            {
                await repo.CreateAsync(new ComparisonRecord
                {
                    ComparisonRecordId = $"cmp_cursor_{i}_{Guid.NewGuid():N}",
                    ComparisonType = "end-to-end-replay",
                    LeftRunId = "L",
                    RightRunId = "R",
                    Format = "json+markdown",
                    SummaryMarkdown = "s",
                    PayloadJson = "{}",
                    CreatedUtc = new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc).AddMinutes(i)
                });
            }
        }

        ComparisonHistoryResponseDto page1 = (await Client.GetFromJsonAsync<ComparisonHistoryResponseDto>(
            "/v1/architecture/comparisons?comparisonType=end-to-end-replay&sortBy=createdUtc&sortDir=desc&limit=2",
            JsonOptions))!;

        page1.Records.Should().HaveCount(2);
        page1.NextCursor.Should().NotBeNullOrWhiteSpace();

        ComparisonHistoryResponseDto page2 = (await Client.GetFromJsonAsync<ComparisonHistoryResponseDto>(
            $"/v1/architecture/comparisons?comparisonType=end-to-end-replay&sortBy=createdUtc&sortDir=desc&limit=2&cursor={Uri.EscapeDataString(page1.NextCursor!)}",
            JsonOptions))!;

        page2.Records.Should().HaveCount(2);
        page1.Records.Select(r => r.ComparisonRecordId)
            .Intersect(page2.Records.Select(r => r.ComparisonRecordId))
            .Should().BeEmpty();
    }

    [Fact]
    public async Task SearchComparisons_InvalidParameters_ReturnsBadRequest()
    {
        HttpResponseMessage badType = await Client.GetAsync("/v1/architecture/comparisons?comparisonType=nope");
        badType.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        HttpResponseMessage badSkip = await Client.GetAsync("/v1/architecture/comparisons?skip=-1");
        badSkip.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        HttpResponseMessage badDates = await Client.GetAsync("/v1/architecture/comparisons?createdFromUtc=2026-01-02T00:00:00Z&createdToUtc=2026-01-01T00:00:00Z");
        badDates.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        HttpResponseMessage badSort = await Client.GetAsync("/v1/architecture/comparisons?sortDir=sideways");
        badSort.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        HttpResponseMessage badSortBy = await Client.GetAsync("/v1/architecture/comparisons?sortBy=anythingElse");
        badSortBy.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task SearchComparisons_FiltersByLabelAndTags()
    {
        using (IServiceScope scope = _factory.Services.CreateScope())
        {
            IComparisonRecordRepository repo = scope.ServiceProvider.GetRequiredService<IComparisonRecordRepository>();
            await repo.CreateAsync(new ComparisonRecord
            {
                ComparisonRecordId = $"cmp_label_{Guid.NewGuid():N}",
                ComparisonType = "export-record-diff",
                LeftExportRecordId = "E1",
                RightExportRecordId = "E2",
                LeftRunId = "RL",
                RightRunId = "RR",
                Format = "json+markdown",
                SummaryMarkdown = "s",
                PayloadJson = "{}",
                CreatedUtc = DateTime.UtcNow.AddMinutes(-10),
                Label = "incident-42",
                Tags = ["incident", "urgent"]
            });
        }

        ComparisonHistoryResponseDto? byLabel = await Client.GetFromJsonAsync<ComparisonHistoryResponseDto>(
            "/v1/architecture/comparisons?label=incident-42&limit=50",
            JsonOptions);
        byLabel.Should().NotBeNull();
        byLabel.Records.Should().Contain(r => r.Label == "incident-42");

        ComparisonHistoryResponseDto? byTags = await Client.GetFromJsonAsync<ComparisonHistoryResponseDto>(
            "/v1/architecture/comparisons?tags=incident,urgent&limit=50",
            JsonOptions);
        byTags.Should().NotBeNull();
        byTags.Records.Should().Contain(r => r.Label == "incident-42");
    }
}

