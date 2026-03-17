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

    public async Task<ComparisonHistoryResult?> SearchComparisonsAsync(
        string? comparisonType,
        string? leftRunId,
        string? rightRunId,
        string? tag,
        int skip,
        int limit,
        CancellationToken ct = default)
    {
        try
        {
            var query = System.Web.HttpUtility.ParseQueryString(string.Empty);
            if (!string.IsNullOrWhiteSpace(comparisonType))
                query["comparisonType"] = comparisonType;
            if (!string.IsNullOrWhiteSpace(leftRunId))
                query["leftRunId"] = leftRunId;
            if (!string.IsNullOrWhiteSpace(rightRunId))
                query["rightRunId"] = rightRunId;
            if (!string.IsNullOrWhiteSpace(tag))
                query["tag"] = tag;
            query["skip"] = skip.ToString();
            query["limit"] = limit.ToString();

            var uri = "/v1/architecture/comparisons";
            var qs = query.ToString();
            if (!string.IsNullOrEmpty(qs))
                uri += "?" + qs;

            var response = await _pipeline.ExecuteAsync(cancellationToken => new ValueTask<HttpResponseMessage>(_http.GetAsync(uri, cancellationToken)), ct);
            var content = await response.Content.ReadAsStringAsync(ct);

            if (!response.IsSuccessStatusCode)
                return null;

            using var doc = JsonDocument.Parse(content);
            var root = doc.RootElement;
            var recordsProp = root.GetProperty("records");
            var list = new List<ComparisonRecordSummary>();
            foreach (var item in recordsProp.EnumerateArray())
            {
                var tagsList = new List<string>();
                if (item.TryGetProperty("tags", out var tagsEl) && tagsEl.ValueKind == JsonValueKind.Array)
                {
                    foreach (var t in tagsEl.EnumerateArray())
                        if (t.ValueKind == JsonValueKind.String)
                            tagsList.Add(t.GetString() ?? "");
                }
                list.Add(new ComparisonRecordSummary
                {
                    ComparisonRecordId = item.GetProperty("comparisonRecordId").GetString() ?? string.Empty,
                    ComparisonType = item.GetProperty("comparisonType").GetString() ?? string.Empty,
                    LeftRunId = item.TryGetProperty("leftRunId", out var lr) && lr.ValueKind != JsonValueKind.Null ? lr.GetString() : null,
                    RightRunId = item.TryGetProperty("rightRunId", out var rr) && rr.ValueKind != JsonValueKind.Null ? rr.GetString() : null,
                    LeftExportRecordId = item.TryGetProperty("leftExportRecordId", out var le) && le.ValueKind != JsonValueKind.Null ? le.GetString() : null,
                    RightExportRecordId = item.TryGetProperty("rightExportRecordId", out var re) && re.ValueKind != JsonValueKind.Null ? re.GetString() : null,
                    CreatedUtc = item.TryGetProperty("createdUtc", out var cu) && cu.ValueKind == JsonValueKind.String
                        ? DateTime.Parse(cu.GetString()!)
                        : default,
                    Label = item.TryGetProperty("label", out var lbl) && lbl.ValueKind != JsonValueKind.Null ? lbl.GetString() : null,
                    Tags = tagsList
                });
            }

            return new ComparisonHistoryResult { Records = list };
        }
        catch
        {
            return null;
        }
    }

    public async Task<bool> ReplayComparisonToFileAsync(
        string comparisonRecordId,
        string format,
        string replayMode,
        string? profile,
        bool persistReplay,
        string? outPath,
        bool force,
        CancellationToken ct = default)
    {
        try
        {
            var uri = $"/v1/architecture/comparisons/{Uri.EscapeDataString(comparisonRecordId)}/replay";
            var body = new
            {
                format,
                replayMode,
                profile,
                persistReplay
            };

            var response = await _pipeline.ExecuteAsync(cancellationToken => new ValueTask<HttpResponseMessage>(_http.PostAsJsonAsync(uri, body, _jsonOptions, cancellationToken)), ct);
            if (!response.IsSuccessStatusCode)
            {
                var contentError = await response.Content.ReadAsStringAsync(ct);
                Console.WriteLine($"Replay failed ({(int)response.StatusCode}): {contentError}");
                return false;
            }

            if (response.Headers.TryGetValues("X-ArchiForge-PersistedReplayRecordId", out var persistedValues))
            {
                var persistedId = persistedValues.FirstOrDefault();
                if (!string.IsNullOrWhiteSpace(persistedId))
                {
                    Console.WriteLine($"PersistedReplayRecordId: {persistedId}");
                }
            }

            var fileName = response.Content.Headers.ContentDisposition?.FileNameStar
                           ?? response.Content.Headers.ContentDisposition?.FileName
                           ?? $"comparison_{comparisonRecordId}.{format}";
            fileName = fileName.Trim('"');

            var targetPath = fileName;
            if (!string.IsNullOrWhiteSpace(outPath))
            {
                if (Directory.Exists(outPath) || outPath.EndsWith(Path.DirectorySeparatorChar) || outPath.EndsWith(Path.AltDirectorySeparatorChar))
                {
                    Directory.CreateDirectory(outPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
                    targetPath = Path.Combine(outPath, fileName);
                }
                else
                {
                    var dir = Path.GetDirectoryName(outPath);
                    if (!string.IsNullOrWhiteSpace(dir))
                        Directory.CreateDirectory(dir);
                    targetPath = outPath;
                }
            }

            if (File.Exists(targetPath) && !force)
            {
                Console.WriteLine($"Refusing to overwrite existing file: {targetPath}");
                Console.WriteLine("Re-run with --force to overwrite, or choose a different --out path.");
                return false;
            }

            var bytes = await response.Content.ReadAsByteArrayAsync(ct);
            await File.WriteAllBytesAsync(targetPath, bytes, ct);
            Console.WriteLine($"Replay exported to {targetPath}");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Replay failed: {ex.Message}");
            return false;
        }
    }

    public sealed class DriftItem
    {
        public string Category { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
        public string? Description { get; set; }
    }

    public sealed class DriftAnalysis
    {
        public bool DriftDetected { get; set; }
        public string Summary { get; set; } = string.Empty;
        public List<DriftItem> Items { get; set; } = [];
    }

    public async Task<DriftAnalysis?> GetComparisonDriftAsync(string comparisonRecordId, CancellationToken ct = default)
    {
        try
        {
            var uri = $"/v1/architecture/comparisons/{Uri.EscapeDataString(comparisonRecordId)}/drift";
            var response = await _pipeline.ExecuteAsync(cancellationToken => new ValueTask<HttpResponseMessage>(_http.PostAsync(uri, null, cancellationToken)), ct);
            var content = await response.Content.ReadAsStringAsync(ct);
            if (!response.IsSuccessStatusCode)
                return null;

            using var doc = JsonDocument.Parse(content);
            var root = doc.RootElement;
            var result = new DriftAnalysis
            {
                DriftDetected = root.TryGetProperty("driftDetected", out var dd) && dd.ValueKind == JsonValueKind.True,
                Summary = root.TryGetProperty("summary", out var s) ? (s.GetString() ?? "") : ""
            };

            if (root.TryGetProperty("items", out var items) && items.ValueKind == JsonValueKind.Array)
            {
                foreach (var it in items.EnumerateArray())
                {
                    result.Items.Add(new DriftItem
                    {
                        Category = it.TryGetProperty("category", out var c) ? (c.GetString() ?? "") : "",
                        Path = it.TryGetProperty("path", out var p) ? (p.GetString() ?? "") : "",
                        Description = it.TryGetProperty("description", out var d) ? d.GetString() : null
                    });
                }
            }

            return result;
        }
        catch
        {
            return null;
        }
    }

    public sealed class ReplayDiagnostics
    {
        public List<ReplayDiagnosticsEntry> RecentReplays { get; set; } = [];
    }

    public sealed class ReplayDiagnosticsEntry
    {
        public DateTime TimestampUtc { get; set; }
        public string ComparisonRecordId { get; set; } = string.Empty;
        public string ComparisonType { get; set; } = string.Empty;
        public string Format { get; set; } = string.Empty;
        public string ReplayMode { get; set; } = string.Empty;
        public long DurationMs { get; set; }
        public bool Success { get; set; }
        public bool MetadataOnly { get; set; }
        public string? PersistedReplayRecordId { get; set; }
        public string? ErrorMessage { get; set; }
    }

    public async Task<ReplayDiagnostics?> GetReplayDiagnosticsAsync(int maxCount, CancellationToken ct = default)
    {
        try
        {
            var uri = $"/v1/architecture/comparisons/diagnostics/replay?maxCount={maxCount}";
            var response = await _pipeline.ExecuteAsync(cancellationToken => new ValueTask<HttpResponseMessage>(_http.GetAsync(uri, cancellationToken)), ct);
            var content = await response.Content.ReadAsStringAsync(ct);
            if (!response.IsSuccessStatusCode)
                return null;

            using var doc = JsonDocument.Parse(content);
            var root = doc.RootElement;
            var result = new ReplayDiagnostics();
            if (root.TryGetProperty("recentReplays", out var arr) && arr.ValueKind == JsonValueKind.Array)
            {
                foreach (var it in arr.EnumerateArray())
                {
                    result.RecentReplays.Add(new ReplayDiagnosticsEntry
                    {
                        TimestampUtc = it.TryGetProperty("timestampUtc", out var t) && t.ValueKind == JsonValueKind.String ? DateTime.Parse(t.GetString()!) : default,
                        ComparisonRecordId = it.TryGetProperty("comparisonRecordId", out var id) ? (id.GetString() ?? "") : "",
                        ComparisonType = it.TryGetProperty("comparisonType", out var ctEl) ? (ctEl.GetString() ?? "") : "",
                        Format = it.TryGetProperty("format", out var f) ? (f.GetString() ?? "") : "",
                        ReplayMode = it.TryGetProperty("replayMode", out var rm) ? (rm.GetString() ?? "") : "",
                        DurationMs = it.TryGetProperty("durationMs", out var dm) && dm.TryGetInt64(out var l) ? l : 0,
                        Success = it.TryGetProperty("success", out var ok) && ok.ValueKind == JsonValueKind.True,
                        MetadataOnly = it.TryGetProperty("metadataOnly", out var mo) && mo.ValueKind == JsonValueKind.True,
                        PersistedReplayRecordId = it.TryGetProperty("persistedReplayRecordId", out var pr) ? pr.GetString() : null,
                        ErrorMessage = it.TryGetProperty("errorMessage", out var em) ? em.GetString() : null
                    });
                }
            }
            return result;
        }
        catch
        {
            return null;
        }
    }

    public async Task<bool> UpdateComparisonRecordAsync(
        string comparisonRecordId,
        string? label,
        IReadOnlyList<string>? tags,
        CancellationToken ct = default)
    {
        try
        {
            var uri = $"/v1/architecture/comparisons/{Uri.EscapeDataString(comparisonRecordId)}";
            var body = new { label, tags = tags ?? (object?)null };
            using var request = new HttpRequestMessage(HttpMethod.Patch, uri)
            {
                Content = JsonContent.Create(body, options: _jsonOptions)
            };
            var response = await _pipeline.ExecuteAsync(cancellationToken => new ValueTask<HttpResponseMessage>(_http.SendAsync(request, cancellationToken)), ct);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public sealed class ComparisonHistoryResult
    {
        public List<ComparisonRecordSummary> Records { get; set; } = [];
    }

    public sealed class ComparisonRecordSummary
    {
        public string ComparisonRecordId { get; set; } = string.Empty;
        public string ComparisonType { get; set; } = string.Empty;
        public string? LeftRunId { get; set; }
        public string? RightRunId { get; set; }
        public string? LeftExportRecordId { get; set; }
        public string? RightExportRecordId { get; set; }
        public DateTime CreatedUtc { get; set; }
        public string? Label { get; set; }
        public List<string> Tags { get; set; } = [];
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
