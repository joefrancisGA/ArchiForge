using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using ArchiForge.Api.Models;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Xunit;

namespace ArchiForge.Api.Tests;

/// <summary>
/// End-to-end: persisted comparison payload is tampered in DB, then verify replay returns 422 (real pipeline, not a stub service).
/// </summary>
public sealed class ComparisonReplayVerifyDriftIntegrationTests : IntegrationTestBase
{
    public ComparisonReplayVerifyDriftIntegrationTests(ArchiForgeApiFactory factory)
        : base(factory)
    {
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task ComparisonReplayVerify_WhenStoredPayloadDriftsFromRegenerated_Returns422()
    {
        var (runId, replayRunId) = await ComparisonReplayTestFixture.CreateRunExecuteCommitReplayAsync(
            Client, JsonOptions, "REQ-VERIFY-DRIFT-001");
        var comparisonRecordId = await ComparisonReplayTestFixture.PersistEndToEndComparisonAsync(
            Client, runId, replayRunId);

        string payloadJson;
        await using (var conn = new SqliteConnection(ArchiForgeApiFactory.SqliteInMemoryConnectionString))
        {
            await conn.OpenAsync();
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT PayloadJson FROM ComparisonRecords WHERE ComparisonRecordId = @id";
            cmd.Parameters.AddWithValue("@id", comparisonRecordId);
            var scalar = await cmd.ExecuteScalarAsync();
            payloadJson = scalar?.ToString() ?? "";
        }

        payloadJson.Should().NotBeNullOrWhiteSpace();
        var node = JsonNode.Parse(payloadJson)!;
        node["leftRunId"] = "tampered-run-id-for-drift";
        var corrupted = node.ToJsonString();

        await using (var conn = new SqliteConnection(ArchiForgeApiFactory.SqliteInMemoryConnectionString))
        {
            await conn.OpenAsync();
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = "UPDATE ComparisonRecords SET PayloadJson = @p WHERE ComparisonRecordId = @id";
            cmd.Parameters.AddWithValue("@p", corrupted);
            cmd.Parameters.AddWithValue("@id", comparisonRecordId);
            (await cmd.ExecuteNonQueryAsync()).Should().Be(1);
        }

        var verifyBody = JsonSerializer.Serialize(new
        {
            format = "markdown",
            replayMode = "verify",
            persistReplay = false
        });
        var verifyResponse = await Client.PostAsync(
            $"/v1/architecture/comparisons/{Uri.EscapeDataString(comparisonRecordId!)}/replay",
            new StringContent(verifyBody, Encoding.UTF8, "application/json"));

        verifyResponse.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        var problem = await verifyResponse.Content.ReadAsStringAsync();
        problem.Should().Contain("comparison-verification-failed");
        problem.Should().Contain("driftDetected");
    }
}
