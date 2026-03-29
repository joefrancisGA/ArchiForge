using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

using ArchiForge.Api.Tests.TestDtos;

namespace ArchiForge.Api.Tests;

/// <summary>Shared helpers for comparison-replay integration tests (run creation, replay, persist).</summary>
public static class ComparisonReplayTestFixture
{
    private static readonly JsonSerializerOptions ReplayBodyJson = new(JsonSerializerDefaults.Web);

    /// <summary>
    /// Creates a run via <c>POST /v1/architecture/request</c>, executes it, and returns the run id (no commit or replay).
    /// </summary>
    /// <param name="client">API test client.</param>
    /// <param name="jsonOptions">Deserializer options for create response DTOs.</param>
    /// <param name="requestId">Stable request id embedded in the architecture request body.</param>
    /// <returns>The new run’s id (hex string).</returns>
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
        HttpResponseMessage executeResponse = await client.PostAsync($"/v1/architecture/run/{runId}/execute", null);
        executeResponse.EnsureSuccessStatusCode();
        return runId;
    }

    /// <summary>
    /// Full golden path through commit and replay with fixed replay options; returns original and replay run ids.
    /// </summary>
    /// <param name="client">API test client.</param>
    /// <param name="jsonOptions">JSON options for DTOs.</param>
    /// <param name="requestId">Request id for the initial architecture request.</param>
    /// <returns>Tuple of <c>(originalRunId, replayRunId)</c>.</returns>
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

        HttpResponseMessage executeResponse = await client.PostAsync($"/v1/architecture/run/{runId}/execute", null);
        executeResponse.EnsureSuccessStatusCode();

        HttpResponseMessage commitResponse = await client.PostAsync($"/v1/architecture/run/{runId}/commit", null);
        commitResponse.EnsureSuccessStatusCode();

        // GoldenManifestVersions.ManifestVersion is globally unique (PK). A fixed override collides when
        // multiple tests share one WebApplicationFactory database (same test class).
        string replayManifestVersion = $"v1r{Guid.NewGuid():N}";

        HttpResponseMessage replayResponse = await client.PostAsync(
            $"/v1/architecture/run/{runId}/replay",
            JsonContent(new
            {
                commitReplay = true,
                executionMode = "Current",
                manifestVersionOverride = replayManifestVersion
            }));
        replayResponse.EnsureSuccessStatusCode();
        ReplayRunResponseDto? replayPayload = await replayResponse.Content.ReadFromJsonAsync<ReplayRunResponseDto>(jsonOptions);
        return (runId, replayPayload!.ReplayRunId);
    }

    /// <summary>
    /// Calls the end-to-end compare summary endpoint with <c>persist: true</c> and returns <c>X-ArchiForge-ComparisonRecordId</c>.
    /// </summary>
    /// <param name="client">API test client.</param>
    /// <param name="leftRunId">Baseline run id (query string).</param>
    /// <param name="rightRunId">Comparison run id (query string).</param>
    /// <returns>New comparison record id from the response header.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the header is missing.</exception>
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
        return new StringContent(JsonSerializer.Serialize(value, ReplayBodyJson), Encoding.UTF8, "application/json");
    }
}
