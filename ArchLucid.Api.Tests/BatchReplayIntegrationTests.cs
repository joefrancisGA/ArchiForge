using System.IO.Compression;
using System.Net;
using System.Text.Json;

using ArchLucid.Api.Http;
using ArchLucid.Api.Services;

using FluentAssertions;

namespace ArchLucid.Api.Tests;

[Trait("Category", "Integration")]
public sealed class BatchReplayIntegrationTests(ArchLucidApiFactory factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task BatchReplay_AllUnknownIds_Returns422WithBatchReplayAllFailedMarkers()
    {
        object body = new
        {
            comparisonRecordIds = new[] { "nonexistent-batch-replay-id-1", "nonexistent-batch-replay-id-2" },
            format = "markdown",
            replayMode = "artifact",
            persistReplay = false
        };
        HttpResponseMessage response = await Client.PostAsync(
            "/v1/architecture/comparisons/replay/batch",
            JsonContent(body));

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        string json = await response.Content.ReadAsStringAsync();
        json.Should().ContainEquivalentOf("batch-replay-all-failed");
        json.Should().ContainEquivalentOf("BATCH_REPLAY_ALL_FAILED");
    }

    [Fact]
    public async Task BatchReplay_MixedValidAndInvalid_ReturnsZipWithManifestAndPartialHeader()
    {
        (string runId, string replayRunId) = await ComparisonReplayTestFixture.CreateRunExecuteCommitReplayAsync(
            Client, JsonOptions, "REQ-BATCH-PARTIAL-001");
        string comparisonRecordId = await ComparisonReplayTestFixture.PersistEndToEndComparisonAsync(
            Client, runId, replayRunId);

        object body = new
        {
            comparisonRecordIds = new[] { comparisonRecordId, "nonexistent-batch-replay-id-zzzzz" },
            format = "markdown",
            replayMode = "artifact",
            persistReplay = false
        };
        HttpResponseMessage response = await Client.PostAsync(
            "/v1/architecture/comparisons/replay/batch",
            JsonContent(body));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/zip");
        response.Headers.TryGetValues(ArchLucidHttpHeaders.BatchReplayPartial, out IEnumerable<string>? values).Should()
            .BeTrue();
        values!.Single().Should().Be("true");

        byte[] zipBytes = await response.Content.ReadAsByteArrayAsync();
        using MemoryStream ms = new(zipBytes);
        await using ZipArchive zip = new(ms, ZipArchiveMode.Read);
        ZipArchiveEntry? manifest = zip.GetEntry(BatchReplayManifestSerializer.ManifestEntryName);
        manifest.Should().NotBeNull();
        await using Stream manifestStream = await manifest.OpenAsync();
        JsonDocument doc = await JsonDocument.ParseAsync(manifestStream);
        doc.RootElement.GetProperty("failed").GetArrayLength().Should().BeGreaterThan(0);
        doc.RootElement.GetProperty("succeeded").GetArrayLength().Should().BeGreaterThan(0);
    }
}
