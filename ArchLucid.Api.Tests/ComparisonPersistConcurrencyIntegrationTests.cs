using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

using ArchLucid.Contracts.Metadata;
using ArchLucid.Persistence.Data.Repositories;

using FluentAssertions;

using Microsoft.Extensions.DependencyInjection;

namespace ArchLucid.Api.Tests;

/// <summary>
///     Parallel <c>POST /v1/architecture/run/compare/end-to-end/summary?persist=true</c>: in-memory
///     <see cref="IComparisonRecordRepository" /> stores one row per successful persist (no deduplication on same run pair),
///     so this test requires five distinct <c>X-ArchLucid-ComparisonRecordId</c> values and five retrievable records.
/// </summary>
[Trait("Suite", "Core")]
[Trait("Category", "Integration")]
[Trait("Category", "Slow")]
public sealed class ComparisonPersistConcurrencyIntegrationTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true, Converters = { new JsonStringEnumConverter(null, true) }
    };

    [Fact]
    public async Task Five_parallel_persisted_compares_produce_five_distinct_in_memory_record_ids()
    {
        await using ArchLucidApiFactory factory = new();
        HttpClient client = factory.CreateClient();
        IntegrationTestBase.WireDefaultSqlIntegrationScopeHeaders(client);

        (string runId, string replayRunId) = await ComparisonReplayTestFixture.CreateRunExecuteCommitReplayAsync(
            client,
            JsonOptions,
            "REQ-COMP-PAR-" + Guid.NewGuid().ToString("N")[..8]);

        IComparisonRecordRepository repository =
            factory.Services.GetRequiredService<IComparisonRecordRepository>();

        const int parallel = 5;
        Task<string>[] tasks = new Task<string>[parallel];

        for (int i = 0; i < parallel; i++)
        {
            tasks[i] = ComparisonReplayTestFixture.PersistEndToEndComparisonAsync(client, runId, replayRunId);
        }

        IReadOnlyList<string> recordIds = await Task.WhenAll(tasks);
        recordIds.Select(static r => r).ToHashSet().Count.Should().Be(parallel, "each persist should create a new record id");

        IReadOnlyList<ComparisonRecord> matches = await repository.SearchAsync(
            comparisonType: null,
            leftRunId: runId,
            rightRunId: replayRunId,
            createdFromUtc: null,
            createdToUtc: null,
            leftExportRecordId: null,
            rightExportRecordId: null,
            label: null,
            tags: null,
            sortBy: null,
            sortDir: null,
            skip: 0,
            limit: 100,
            cancellationToken: CancellationToken.None);

        int pairMatches = matches.Count(
            m => string.Equals(m.LeftRunId, runId, StringComparison.Ordinal)
            && string.Equals(m.RightRunId, replayRunId, StringComparison.Ordinal));
        pairMatches.Should().BeGreaterThanOrEqualTo(parallel);
    }
}
