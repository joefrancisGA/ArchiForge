using System.Net.Http.Json;

using ArchiForge.Contracts.Metadata;
using ArchiForge.Data.Repositories;

using FluentAssertions;

using Microsoft.Extensions.DependencyInjection;

namespace ArchiForge.Api.Tests;

[Trait("Category", "Integration")]
public sealed class ArchitectureComparisonTaggingTests(ArchiForgeApiFactory factory) : IntegrationTestBase(factory)
{
    private readonly ArchiForgeApiFactory _factory = factory;

    [Fact]
    public async Task UpdateComparisonRecord_UpdatesLabelAndTags_ThenSearchFindsIt()
    {
        string id = $"cmp_update_{Guid.NewGuid():N}";

        using (IServiceScope scope = _factory.Services.CreateScope())
        {
            IComparisonRecordRepository repo = scope.ServiceProvider.GetRequiredService<IComparisonRecordRepository>();
            await repo.CreateAsync(new ComparisonRecord
            {
                ComparisonRecordId = id,
                ComparisonType = "export-record-diff",
                LeftExportRecordId = "E1",
                RightExportRecordId = "E2",
                LeftRunId = "L",
                RightRunId = "R",
                Format = "json+markdown",
                SummaryMarkdown = "s",
                PayloadJson = "{}",
                CreatedUtc = DateTime.UtcNow.AddMinutes(-5)
            });
        }

        HttpResponseMessage patch = await Client.PatchAsJsonAsync(
            $"/v1/architecture/comparisons/{id}",
            new
            {
                label = "incident-99",
                tags = new[] { "incident", "urgent" }
            });

        patch.EnsureSuccessStatusCode();

        ComparisonRecordResponseDto? updated = await patch.Content.ReadFromJsonAsync<ComparisonRecordResponseDto>(JsonOptions);
        updated!.Record.Label.Should().Be("incident-99");
        updated.Record.Tags.Should().Contain(["incident", "urgent"]);

        ComparisonHistoryResponseDto? byTag = await Client.GetFromJsonAsync<ComparisonHistoryResponseDto>(
            "/v1/architecture/comparisons?tags=incident,urgent&limit=50",
            JsonOptions);

        byTag!.Records.Should().Contain(r => r.ComparisonRecordId == id);
    }
}

