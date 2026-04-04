using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

using ArchiForge.Persistence.Data.Repositories;

using FluentAssertions;

using Microsoft.Extensions.DependencyInjection;

namespace ArchiForge.Api.Tests;

/// <summary>
/// End-to-end: persisted comparison payload is tampered, then verify replay returns 422 (real pipeline, not a stub service).
/// </summary>
/// <remarks>
/// Default integration hosts use <c>ArchiForge:StorageProvider=InMemory</c>, so <c>ComparisonRecords</c> are not written to SQL.
/// Payload is read via GET, then mutated in the in-memory repository through <see cref="InMemoryComparisonRecordRepository.ReplacePayloadJsonForIntegrationTest"/>.
/// </remarks>
[Trait("Category", "Integration")]
[Trait("Category", "Slow")]
public sealed class ComparisonReplayVerifyDriftIntegrationTests(ArchiForgeApiFactory factory)
    : IntegrationTestBase(factory)
{
    [Fact]
    public async Task ComparisonReplayVerify_WhenStoredPayloadDriftsFromRegenerated_Returns422()
    {
        (string runId, string replayRunId) = await ComparisonReplayTestFixture.CreateRunExecuteCommitReplayAsync(
            Client, JsonOptions, "REQ-VERIFY-DRIFT-001");
        string comparisonRecordId = await ComparisonReplayTestFixture.PersistEndToEndComparisonAsync(
            Client, runId, replayRunId);

        HttpResponseMessage getResponse = await Client.GetAsync(
            $"/v1/architecture/comparisons/{Uri.EscapeDataString(comparisonRecordId)}");
        getResponse.EnsureSuccessStatusCode();
        ComparisonRecordResponseDto? dto = await getResponse.Content.ReadFromJsonAsync<ComparisonRecordResponseDto>(JsonOptions);
        string payloadJson = dto!.Record.PayloadJson;
        payloadJson.Should().NotBeNullOrWhiteSpace();

        JsonNode node = JsonNode.Parse(payloadJson)!;
        node["leftRunId"] = "tampered-run-id-for-drift";
        string corrupted = node.ToJsonString();

        IComparisonRecordRepository repo = Factory.Services.GetRequiredService<IComparisonRecordRepository>();
        InMemoryComparisonRecordRepository memoryRepo = repo as InMemoryComparisonRecordRepository
            ?? throw new InvalidOperationException(
                "Expected IComparisonRecordRepository to be InMemoryComparisonRecordRepository (integration host with InMemory storage).");
        memoryRepo.ReplacePayloadJsonForIntegrationTest(comparisonRecordId, corrupted);

        string verifyBody = JsonSerializer.Serialize(new
        {
            format = "markdown",
            replayMode = "verify",
            persistReplay = false
        });
        HttpResponseMessage verifyResponse = await Client.PostAsync(
            $"/v1/architecture/comparisons/{Uri.EscapeDataString(comparisonRecordId)}/replay",
            new StringContent(verifyBody, Encoding.UTF8, "application/json"));

        verifyResponse.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        string problem = await verifyResponse.Content.ReadAsStringAsync();
        problem.Should().Contain("comparison-verification-failed");
        problem.Should().Contain("driftDetected");
    }
}
