using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace ArchiForge.Api.Tests;

/// <summary>Shared helpers for comparison-replay integration tests (run creation, replay, persist).</summary>
public static class ComparisonReplayTestFixture
{
    /// <summary>Creates a run, executes, commits, replays; returns (runId, replayRunId).</summary>
    public static async Task<(string RunId, string ReplayRunId)> CreateRunExecuteCommitReplayAsync(
        HttpClient client,
        JsonSerializerOptions jsonOptions,
        string requestId = "REQ-FIXTURE-001")
    {
        var createResponse = await client.PostAsync(
            "/v1/architecture/request",
            JsonContent(TestRequestFactory.CreateArchitectureRequest(requestId)));
        createResponse.EnsureSuccessStatusCode();
        var created = await createResponse.Content.ReadFromJsonAsync<CreateRunResponseDto>(jsonOptions);
        var runId = created!.Run.RunId;

        await client.PostAsync($"/v1/architecture/run/{runId}/execute", null);
        await client.PostAsync($"/v1/architecture/run/{runId}/commit", null);

        var replayResponse = await client.PostAsync(
            $"/v1/architecture/run/{runId}/replay",
            JsonContent(new
            {
                commitReplay = true,
                executionMode = "Current",
                manifestVersionOverride = "v1-replay-fixture"
            }));
        replayResponse.EnsureSuccessStatusCode();
        var replayPayload = await replayResponse.Content.ReadFromJsonAsync<ReplayRunResponseDto>(jsonOptions);
        return (runId, replayPayload!.ReplayRunId);
    }

    /// <summary>Persists an end-to-end comparison; returns comparisonRecordId from header.</summary>
    public static async Task<string> PersistEndToEndComparisonAsync(
        HttpClient client,
        string leftRunId,
        string rightRunId)
    {
        var persist = await client.PostAsync(
            $"/v1/architecture/run/compare/end-to-end/summary?leftRunId={Uri.EscapeDataString(leftRunId)}&rightRunId={Uri.EscapeDataString(rightRunId)}",
            new StringContent("""{"persist":true}""", Encoding.UTF8, "application/json"));
        persist.EnsureSuccessStatusCode();
        var comparisonRecordId = persist.Headers.GetValues("X-ArchiForge-ComparisonRecordId").FirstOrDefault();
        if (string.IsNullOrEmpty(comparisonRecordId))
            throw new InvalidOperationException("X-ArchiForge-ComparisonRecordId header missing.");
        return comparisonRecordId;
    }

    private static StringContent JsonContent(object value)
    {
        return new StringContent(JsonSerializer.Serialize(value), Encoding.UTF8, "application/json");
    }
}
