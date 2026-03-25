using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

using ArchiForge.Api.Tests.TestDtos;

namespace ArchiForge.Api.Tests;

/// <summary>Shared helpers for comparison-replay integration tests (run creation, replay, persist).</summary>
public static class ComparisonReplayTestFixture
{
    /// <summary>Creates a run and executes; returns runId (no commit or replay).</summary>
    public static async Task<string> CreateRunAndExecuteAsync(
        HttpClient client,
        JsonSerializerOptions jsonOptions,
        string requestId = "REQ-FIXTURE-001")
    {
        HttpResponseMessage createResponse = await client.PostAsync(
            "/v1/architecture/request",
            JsonContent(TestRequestFactory.CreateArchitectureRequest(requestId)));
        createResponse.EnsureSuccessStatusCode();
        CreateRunResponseDto? created = await createResponse.Content.ReadFromJsonAsync<CreateRunResponseDto>(jsonOptions);
        string runId = created!.Run.RunId;
        await client.PostAsync($"/v1/architecture/run/{runId}/execute", null);
        return runId;
    }

    /// <summary>Creates a run, executes, commits, replays; returns (runId, replayRunId).</summary>
    public static async Task<(string RunId, string ReplayRunId)> CreateRunExecuteCommitReplayAsync(
        HttpClient client,
        JsonSerializerOptions jsonOptions,
        string requestId = "REQ-FIXTURE-001")
    {
        HttpResponseMessage createResponse = await client.PostAsync(
            "/v1/architecture/request",
            JsonContent(TestRequestFactory.CreateArchitectureRequest(requestId)));
        createResponse.EnsureSuccessStatusCode();
        CreateRunResponseDto? created = await createResponse.Content.ReadFromJsonAsync<CreateRunResponseDto>(jsonOptions);
        string runId = created!.Run.RunId;

        await client.PostAsync($"/v1/architecture/run/{runId}/execute", null);
        await client.PostAsync($"/v1/architecture/run/{runId}/commit", null);

        HttpResponseMessage replayResponse = await client.PostAsync(
            $"/v1/architecture/run/{runId}/replay",
            JsonContent(new
            {
                commitReplay = true,
                executionMode = "Current",
                manifestVersionOverride = "v1-replay-fixture"
            }));
        replayResponse.EnsureSuccessStatusCode();
        ReplayRunResponseDto? replayPayload = await replayResponse.Content.ReadFromJsonAsync<ReplayRunResponseDto>(jsonOptions);
        return (runId, replayPayload!.ReplayRunId);
    }

    /// <summary>Persists an end-to-end comparison; returns comparisonRecordId from header.</summary>
    public static async Task<string> PersistEndToEndComparisonAsync(
        HttpClient client,
        string leftRunId,
        string rightRunId)
    {
        HttpResponseMessage persist = await client.PostAsync(
            $"/v1/architecture/run/compare/end-to-end/summary?leftRunId={Uri.EscapeDataString(leftRunId)}&rightRunId={Uri.EscapeDataString(rightRunId)}",
            new StringContent("""{"persist":true}""", Encoding.UTF8, "application/json"));
        persist.EnsureSuccessStatusCode();
        string? comparisonRecordId = persist.Headers.GetValues("X-ArchiForge-ComparisonRecordId").FirstOrDefault();
        return string.IsNullOrEmpty(comparisonRecordId) ? throw new InvalidOperationException("X-ArchiForge-ComparisonRecordId header missing.") : comparisonRecordId;
    }

    private static StringContent JsonContent(object value)
    {
        return new StringContent(JsonSerializer.Serialize(value), Encoding.UTF8, "application/json");
    }
}
