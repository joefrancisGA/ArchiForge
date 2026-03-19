using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using ArchiForge.Contracts.Metadata;
using ArchiForge.Data.Repositories;

namespace ArchiForge.Api.Tests;

[Trait("Category", "Integration")]
public sealed class ArchitectureComparisonSearchTests(ArchiForgeApiFactory factory) : IntegrationTestBase(factory)
{
    private readonly ArchiForgeApiFactory _factory = factory;

    [Fact]
    public async Task SearchComparisons_PagingDoesNotOverlap_AndSortAscWorks()
    {
        var ids = new List<string>();

        using (var scope = _factory.Services.CreateScope())
        {
            var repo = scope.ServiceProvider.GetRequiredService<IComparisonRecordRepository>();
            for (var i = 0; i < 6; i++)
            {
                var record = new ComparisonRecord
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

        var page1 = await Client.GetFromJsonAsync<ComparisonHistoryResponseDto>(
            "/v1/architecture/comparisons?comparisonType=end-to-end-replay&limit=2&skip=0",
            JsonOptions);
        page1.Should().NotBeNull();
        page1!.Records.Should().HaveCount(2);

        var page2 = await Client.GetFromJsonAsync<ComparisonHistoryResponseDto>(
            "/v1/architecture/comparisons?comparisonType=end-to-end-replay&limit=2&skip=2",
            JsonOptions);
        page2.Should().NotBeNull();
        page2!.Records.Should().HaveCount(2);

        page1.Records.Select(r => r.ComparisonRecordId)
            .Intersect(page2.Records.Select(r => r.ComparisonRecordId))
            .Should().BeEmpty();

        // Ascending sort: first item should be the earliest CreatedUtc (minute 0)
        var asc = await Client.GetFromJsonAsync<ComparisonHistoryResponseDto>(
            "/v1/architecture/comparisons?comparisonType=end-to-end-replay&limit=6&skip=0&sortDir=asc",
            JsonOptions);
        asc.Should().NotBeNull();
        asc!.Records.Should().HaveCount(6);
        asc.Records.First().ComparisonRecordId.Should().Be(ids[0]);
    }

    [Fact]
    public async Task SearchComparisons_CursorPaging_ReturnsNextPage()
    {
        ComparisonHistoryResponseDto page1;
        ComparisonHistoryResponseDto page2;

        using (var scope = _factory.Services.CreateScope())
        {
            var repo = scope.ServiceProvider.GetRequiredService<IComparisonRecordRepository>();
            for (var i = 0; i < 5; i++)
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

        page1 = (await Client.GetFromJsonAsync<ComparisonHistoryResponseDto>(
            "/v1/architecture/comparisons?comparisonType=end-to-end-replay&sortBy=createdUtc&sortDir=desc&limit=2",
            JsonOptions))!;

        page1.Records.Should().HaveCount(2);
        page1.NextCursor.Should().NotBeNullOrWhiteSpace();

        page2 = (await Client.GetFromJsonAsync<ComparisonHistoryResponseDto>(
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
        var badType = await Client.GetAsync("/v1/architecture/comparisons?comparisonType=nope");
        badType.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var badSkip = await Client.GetAsync("/v1/architecture/comparisons?skip=-1");
        badSkip.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var badDates = await Client.GetAsync("/v1/architecture/comparisons?createdFromUtc=2026-01-02T00:00:00Z&createdToUtc=2026-01-01T00:00:00Z");
        badDates.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var badSort = await Client.GetAsync("/v1/architecture/comparisons?sortDir=sideways");
        badSort.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var badSortBy = await Client.GetAsync("/v1/architecture/comparisons?sortBy=anythingElse");
        badSortBy.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task SearchComparisons_FiltersByLabelAndTags()
    {
        using (var scope = _factory.Services.CreateScope())
        {
            var repo = scope.ServiceProvider.GetRequiredService<IComparisonRecordRepository>();
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

        var byLabel = await Client.GetFromJsonAsync<ComparisonHistoryResponseDto>(
            "/v1/architecture/comparisons?label=incident-42&limit=50",
            JsonOptions);
        byLabel.Should().NotBeNull();
        byLabel!.Records.Should().Contain(r => r.Label == "incident-42");

        var byTags = await Client.GetFromJsonAsync<ComparisonHistoryResponseDto>(
            "/v1/architecture/comparisons?tags=incident,urgent&limit=50",
            JsonOptions);
        byTags.Should().NotBeNull();
        byTags!.Records.Should().Contain(r => r.Label == "incident-42");
    }
}

