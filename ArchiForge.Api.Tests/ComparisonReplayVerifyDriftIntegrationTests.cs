using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

using FluentAssertions;

using Microsoft.Data.Sqlite;

namespace ArchiForge.Api.Tests;

/// <summary>
/// End-to-end: persisted comparison payload is tampered in DB, then verify replay returns 422 (real pipeline, not a stub service).
/// </summary>
[Trait("Category", "Integration")]
public sealed class ComparisonReplayVerifyDriftIntegrationTests(ArchiForgeApiFactory factory)
    : IntegrationTestBase(factory)
{
    [Fact]
    [Trait("Category", "Integration")]
    public async Task ComparisonReplayVerify_WhenStoredPayloadDriftsFromRegenerated_Returns422()
    {
        (string runId, string replayRunId) = await ComparisonReplayTestFixture.CreateRunExecuteCommitReplayAsync(
            Client, JsonOptions, "REQ-VERIFY-DRIFT-001");
        string comparisonRecordId = await ComparisonReplayTestFixture.PersistEndToEndComparisonAsync(
            Client, runId, replayRunId);

        string payloadJson;
        await using (SqliteConnection conn = new(ArchiForgeApiFactory.SqliteInMemoryConnectionString))
        {
            await conn.OpenAsync();
            await using SqliteCommand cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT PayloadJson FROM ComparisonRecords WHERE ComparisonRecordId = @id";
            cmd.Parameters.AddWithValue("@id", comparisonRecordId);
            object? scalar = await cmd.ExecuteScalarAsync();
            payloadJson = scalar?.ToString() ?? "";
        }

        payloadJson.Should().NotBeNullOrWhiteSpace();
        JsonNode node = JsonNode.Parse(payloadJson)!;
        node["leftRunId"] = "tampered-run-id-for-drift";
        string corrupted = node.ToJsonString();

        await using (SqliteConnection conn = new(ArchiForgeApiFactory.SqliteInMemoryConnectionString))
        {
            await conn.OpenAsync();
            await using SqliteCommand cmd = conn.CreateCommand();
            cmd.CommandText = "UPDATE ComparisonRecords SET PayloadJson = @p WHERE ComparisonRecordId = @id";
            cmd.Parameters.AddWithValue("@p", corrupted);
            cmd.Parameters.AddWithValue("@id", comparisonRecordId);
            (await cmd.ExecuteNonQueryAsync()).Should().Be(1);
        }

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
