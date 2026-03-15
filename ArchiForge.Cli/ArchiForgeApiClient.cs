using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ArchiForge.Contracts.Agents;
using ArchiForge.Contracts.Requests;
using Polly;
using Polly.Retry;

namespace ArchiForge;

/// <summary>
/// HTTP client for the ArchiForge API with resilience (retry on transient failures).
/// </summary>
public sealed class ArchiForgeApiClient
{
    private readonly HttpClient _http;
    private readonly ResiliencePipeline<HttpResponseMessage> _pipeline;
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        WriteIndented = false
    };

    public ArchiForgeApiClient(string baseUrl)
    {
        if (string.IsNullOrWhiteSpace(baseUrl))
            throw new ArgumentException("Base URL is required.", nameof(baseUrl));

        baseUrl = baseUrl.TrimEnd('/');
        _http = new HttpClient
        {
            BaseAddress = new Uri(baseUrl),
            Timeout = TimeSpan.FromSeconds(30)
        };
        _http.DefaultRequestHeaders.Add("Accept", "application/json");

        _pipeline = new ResiliencePipelineBuilder<HttpResponseMessage>()
            .AddRetry(new RetryStrategyOptions<HttpResponseMessage>
            {
                MaxRetryAttempts = 3,
                Delay = TimeSpan.FromSeconds(1),
                BackoffType = DelayBackoffType.Exponential,
                UseJitter = true,
                ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                    .Handle<HttpRequestException>()
                    .HandleResult(r => (int)r.StatusCode >= 500 || r.StatusCode == HttpStatusCode.RequestTimeout || r.StatusCode == (HttpStatusCode)429)
            })
            .Build();
    }

    /// <summary>
    /// Constructor for testing: use a provided HttpClient (e.g. with a mock handler).
    /// No retry pipeline is used so tests get deterministic behavior.
    /// </summary>
    public ArchiForgeApiClient(HttpClient httpClient)
    {
        _http = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _pipeline = new ResiliencePipelineBuilder<HttpResponseMessage>().Build();
    }

    public static string GetDefaultBaseUrl() =>
        Environment.GetEnvironmentVariable("ARCHIFORGE_API_URL") ?? "http://localhost:5128";

    /// <summary>Resolve API base URL: config.ApiUrl (when set) > ARCHIFORGE_API_URL env > default.</summary>
    public static string ResolveBaseUrl(ArchiForgeProjectScaffolder.ArchiForgeConfig? config)
    {
        if (!string.IsNullOrWhiteSpace(config?.ApiUrl))
            return config.ApiUrl.Trim().TrimEnd('/');
        return GetDefaultBaseUrl().TrimEnd('/');
    }

    /// <summary>
    /// Check connectivity to the ArchiForge API (GET /health). Returns true if the API is reachable and healthy.
    /// </summary>
    public async Task<bool> CheckHealthAsync(CancellationToken ct = default)
    {
        try
        {
            var response = await _http.GetAsync("/health", ct);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Create an architecture run by submitting an ArchitectureRequest.
    /// </summary>
    public async Task<CreateRunResult> CreateRunAsync(ArchitectureRequest request, CancellationToken ct = default)
    {
        try
        {
            var response = await _pipeline.ExecuteAsync(cancellationToken => new ValueTask<HttpResponseMessage>(_http.PostAsJsonAsync("/v1/architecture/request", request, _jsonOptions, cancellationToken)), ct);
            var content = await response.Content.ReadAsStringAsync(ct);

            if (response.IsSuccessStatusCode)
            {
                var created = JsonSerializer.Deserialize<CreateRunResponse>(content, _jsonOptions);
                return CreateRunResult.Ok(created);
            }

            var error = TryParseError(content);
            return CreateRunResult.Fail((int)response.StatusCode, error ?? content);
        }
        catch (HttpRequestException ex)
        {
            return CreateRunResult.Fail(null, $"Cannot connect to ArchiForge API: {ex.Message}");
        }
        catch (TaskCanceledException)
        {
            return CreateRunResult.Fail(null, "Request timed out.");
        }
    }

    /// <summary>
    /// Submit an agent result for a run.
    /// </summary>
    public async Task<SubmitResultResult?> SubmitAgentResultAsync(string runId, AgentResult result, CancellationToken ct = default)
    {
        try
        {
            result.RunId = runId;
            var request = new SubmitAgentResultRequest { Result = result };
            var uri = $"/v1/architecture/run/{Uri.EscapeDataString(runId)}/result";
            var response = await _pipeline.ExecuteAsync(cancellationToken => new ValueTask<HttpResponseMessage>(_http.PostAsJsonAsync(uri, request, _jsonOptions, cancellationToken)), ct);
            var content = await response.Content.ReadAsStringAsync(ct);

            if (!response.IsSuccessStatusCode)
            {
                var error = TryParseError(content);
                return new SubmitResultResult(false, null, error ?? content);
            }

            var parsed = JsonSerializer.Deserialize<SubmitResultResponse>(content, _jsonOptions);
            return new SubmitResultResult(true, parsed?.ResultId, null);
        }
        catch (HttpRequestException ex)
        {
            return new SubmitResultResult(false, null, $"Cannot connect to ArchiForge API: {ex.Message}");
        }
        catch (TaskCanceledException)
        {
            return new SubmitResultResult(false, null, "Request timed out.");
        }
    }

    /// <summary>
    /// Get run status, tasks, and results.
    /// </summary>
    public async Task<GetRunResult?> GetRunAsync(string runId, CancellationToken ct = default)
    {
        try
        {
            var uri = $"/v1/architecture/run/{Uri.EscapeDataString(runId)}";
            var response = await _pipeline.ExecuteAsync(cancellationToken => new ValueTask<HttpResponseMessage>(_http.GetAsync(uri, cancellationToken)), ct);
            var content = await response.Content.ReadAsStringAsync(ct);

            if (!response.IsSuccessStatusCode)
                return null;

            return JsonSerializer.Deserialize<GetRunResult>(content, _jsonOptions);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Commit a run: merge agent results and produce a versioned manifest.
    /// </summary>
    public async Task<CommitRunResult?> CommitRunAsync(string runId, CancellationToken ct = default)
    {
        try
        {
            var uri = $"/v1/architecture/run/{Uri.EscapeDataString(runId)}/commit";
            var response = await _pipeline.ExecuteAsync(cancellationToken => new ValueTask<HttpResponseMessage>(_http.PostAsync(uri, null, cancellationToken)), ct);
            var content = await response.Content.ReadAsStringAsync(ct);

            if (!response.IsSuccessStatusCode)
            {
                var error = TryParseError(content);
                return new CommitRunResult(false, null, error ?? content);
            }

            var result = JsonSerializer.Deserialize<CommitRunResponse>(content, _jsonOptions);
            return new CommitRunResult(true, result, null);
        }
        catch (HttpRequestException ex)
        {
            return new CommitRunResult(false, null, $"Cannot connect to ArchiForge API: {ex.Message}");
        }
        catch (TaskCanceledException)
        {
            return new CommitRunResult(false, null, "Request timed out.");
        }
    }

    /// <summary>
    /// Seed fake results for a run (Development only).
    /// </summary>
    public async Task<SeedFakeResultsResult?> SeedFakeResultsAsync(string runId, CancellationToken ct = default)
    {
        try
        {
            var uri = $"/v1/architecture/run/{Uri.EscapeDataString(runId)}/seed-fake-results";
            var response = await _pipeline.ExecuteAsync(cancellationToken => new ValueTask<HttpResponseMessage>(_http.PostAsync(uri, null, cancellationToken)), ct);
            var content = await response.Content.ReadAsStringAsync(ct);

            if (!response.IsSuccessStatusCode)
            {
                var error = TryParseError(content);
                return new SeedFakeResultsResult(false, 0, error ?? content);
            }

            var result = JsonSerializer.Deserialize<SeedFakeResultsResponse>(content, _jsonOptions);
            return new SeedFakeResultsResult(true, result?.ResultCount ?? 0, null);
        }
        catch (HttpRequestException ex)
        {
            return new SeedFakeResultsResult(false, 0, $"Cannot connect to ArchiForge API: {ex.Message}");
        }
        catch (TaskCanceledException)
        {
            return new SeedFakeResultsResult(false, 0, "Request timed out.");
        }
    }

    /// <summary>
    /// Get manifest by version.
    /// </summary>
    public async Task<object?> GetManifestAsync(string version, CancellationToken ct = default)
    {
        try
        {
            var uri = $"/v1/architecture/manifest/{Uri.EscapeDataString(version)}";
            var response = await _pipeline.ExecuteAsync(cancellationToken => new ValueTask<HttpResponseMessage>(_http.GetAsync(uri, cancellationToken)), ct);
            var content = await response.Content.ReadAsStringAsync(ct);

            if (!response.IsSuccessStatusCode)
                return null;

            return JsonSerializer.Deserialize<JsonElement>(content);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Parse error message from JSON. Supports RFC 7807 Problem Details (detail, title) and legacy (error, errors).
    /// </summary>
    private static string? TryParseError(string json)
    {
        try
        {
            var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            if (root.TryGetProperty("detail", out var detail))
                return detail.GetString();
            if (root.TryGetProperty("error", out var err))
                return err.GetString();
            if (root.TryGetProperty("errors", out var errs) && errs.ValueKind == JsonValueKind.Array)
                return string.Join("; ", errs.EnumerateArray().Select(e => e.GetString()).Where(s => !string.IsNullOrEmpty(s)));
            if (root.TryGetProperty("title", out var title))
                return title.GetString();
        }
        catch { }
        return null;
    }

    public sealed record CreateRunResult(bool Success, CreateRunResponse? Response, string? Error, int? StatusCode)
    {
        public static CreateRunResult Ok(CreateRunResponse? r) => new(true, r, null, null);
        public static CreateRunResult Fail(int? statusCode, string error) => new(false, null, error, statusCode);
    }

    public sealed class CreateRunResponse
    {
        public RunInfo Run { get; set; } = new();
        public List<AgentTaskInfo> Tasks { get; set; } = [];
    }

    public sealed class RunInfo
    {
        public string RunId { get; set; } = "";
        public string RequestId { get; set; } = "";
        public int Status { get; set; }
        public DateTime CreatedUtc { get; set; }
        public DateTime? CompletedUtc { get; set; }
        public string? CurrentManifestVersion { get; set; }
    }

    public sealed class AgentTaskInfo
    {
        public string TaskId { get; set; } = "";
        public string RunId { get; set; } = "";
        public int AgentType { get; set; }
        public string Objective { get; set; } = "";
        public int Status { get; set; }
    }

    public sealed class GetRunResult
    {
        public RunInfo Run { get; set; } = new();
        public List<AgentTaskInfo> Tasks { get; set; } = [];
        public List<object> Results { get; set; } = [];
    }

    public sealed record CommitRunResult(bool Success, CommitRunResponse? Response, string? Error);

    public sealed class CommitRunResponse
    {
        public ManifestInfo Manifest { get; set; } = new();
        public List<string> Warnings { get; set; } = [];
    }

    public sealed class ManifestInfo
    {
        public string RunId { get; set; } = "";
        public string SystemName { get; set; } = "";
        public ManifestMetadataInfo Metadata { get; set; } = new();
    }

    public sealed class ManifestMetadataInfo
    {
        public string ManifestVersion { get; set; } = "";
    }

    public sealed record SeedFakeResultsResult(bool Success, int ResultCount, string? Error);

    public sealed class SeedFakeResultsResponse
    {
        public string Message { get; set; } = "";
        public string RunId { get; set; } = "";
        public int ResultCount { get; set; }
    }

    public sealed record SubmitResultResult(bool Success, string? ResultId, string? Error);

    public sealed class SubmitResultResponse
    {
        public string Message { get; set; } = "";
        public string RunId { get; set; } = "";
        public string ResultId { get; set; } = "";
    }

    internal sealed class SubmitAgentResultRequest
    {
        public AgentResult Result { get; set; } = new();
    }
}
