using System.Diagnostics.CodeAnalysis;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

using ArchLucid.Cli.Commands;
using ArchLucid.Contracts.Agents;
using ArchLucid.Contracts.Requests;

using Gen = ArchLucid.Api.Client.Generated;

namespace ArchLucid.Cli;

/// <summary>
/// CLI-facing HTTP surface backed by the NSwag-generated <see cref="ArchLucidApiClient"/> (OpenAPI v1 contract).
/// Binary comparison replay/zip exports still use raw <see cref="HttpClient"/> because the OpenAPI model uses
/// <c>FileContentResult</c> JSON rather than octet-stream bodies.
/// </summary>
[ExcludeFromCodeCoverage(Justification = "HTTP client against live API; covered by CLI integration tests.")]
public sealed class ArchLucidApiClient
{
    private readonly HttpClient _http;
    private readonly Gen.ArchLucidApiClient _api;
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        WriteIndented = false,
    };

    public ArchLucidApiClient(string baseUrl, ArchLucidProjectScaffolder.ArchLucidCliConfig? cliConfig = null)
    {
        string? invalidReason = GetInvalidApiBaseUrlReason(baseUrl);
        if (invalidReason is not null)
        {
            throw new ArgumentException(invalidReason, nameof(baseUrl));
        }

        string normalized = baseUrl.Trim().TrimEnd('/');
        ArchLucidProjectScaffolder.ArchLucidCliConfig? effectiveConfig =
            cliConfig ?? CliCommandShared.TryLoadConfigFromCwd();
        CliResilienceOptions httpResilience = CliResilienceOptions.FromCliConfig(effectiveConfig);
        _http = CreateHttpClient(normalized, useRetry: true, httpResilience);
        _api = new Gen.ArchLucidApiClient(_http) { BaseUrl = normalized + "/" };
    }

    /// <summary>
    /// Constructor for testing: use a provided HttpClient (e.g. with a mock handler).
    /// No retry pipeline is used so tests get deterministic behavior.
    /// </summary>
    public ArchLucidApiClient(HttpClient httpClient)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        _http = httpClient;
        string baseUrl = httpClient.BaseAddress?.ToString().Trim().TrimEnd('/') ?? "http://localhost";
        _api = new Gen.ArchLucidApiClient(_http) { BaseUrl = baseUrl + "/" };
    }

    private static HttpClient CreateHttpClient(string normalizedBaseUrl, bool useRetry, CliResilienceOptions? httpResilience = null)
    {
        HttpMessageHandler inner = new HttpClientHandler();
        if (useRetry)
        {
            inner = new CliRetryDelegatingHandler(httpResilience) { InnerHandler = inner };
        }

        HttpClient http = new(inner, disposeHandler: true)
        {
            BaseAddress = new Uri(normalizedBaseUrl + "/"),
            Timeout = TimeSpan.FromSeconds(30),
        };
        http.DefaultRequestHeaders.Add("Accept", "application/json");

        string? apiKey = Environment.GetEnvironmentVariable("ARCHLUCID_API_KEY");
        if (string.IsNullOrWhiteSpace(apiKey))
            return http;
        http.DefaultRequestHeaders.Remove("X-Api-Key");
        http.DefaultRequestHeaders.Add("X-Api-Key", apiKey);

        return http;
    }

    public static string GetDefaultBaseUrl() =>
        Environment.GetEnvironmentVariable("ARCHLUCID_API_URL") ?? "http://localhost:5128";

    /// <summary>
    /// Returns a human-readable reason when the value cannot be used as an absolute HTTP API base URL, or null when valid.
    /// </summary>
    public static string? GetInvalidApiBaseUrlReason(string? baseUrl)
    {
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            return "API base URL is empty. Set apiUrl in archlucid.json in the project folder or ARCHLUCID_API_URL (example: http://localhost:5128).";
        }

        string trimmed = baseUrl.Trim();
        if (!Uri.TryCreate(trimmed, UriKind.Absolute, out Uri? uri))
        {
            return $"API base URL is not a valid absolute URL: '{trimmed}'. Use http:// or https:// with a host (example: http://localhost:5128).";
        }

        if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
        {
            return $"API base URL must use http or https (got '{uri.Scheme}').";
        }

        return null;
    }

    /// <summary>Resolve API base URL: config.ApiUrl (when set) &gt; ARCHLUCID_API_URL &gt; default.</summary>
    public static string ResolveBaseUrl(ArchLucidProjectScaffolder.ArchLucidCliConfig? config)
    {
        return !string.IsNullOrWhiteSpace(config?.ApiUrl) ? config.ApiUrl.Trim().TrimEnd('/') : GetDefaultBaseUrl().TrimEnd('/');
    }

    private static void LogCliFailure(string operation, Exception ex) =>
        Console.Error.WriteLine($"[ArchLucid CLI] {operation} failed: {ex.GetType().Name}: {ex.Message}");

    /// <summary>
    /// Calls <c>GET /version</c> and returns the raw JSON body for operator diagnostics.
    /// </summary>
    public async Task<string?> GetVersionJsonAsync(CancellationToken ct = default)
    {
        try
        {
            Gen.BuildInfoResponse info = await _api.VersionAsync(ct);

            return JsonSerializer.Serialize(info, _jsonOptions);
        }
        catch (Exception ex)
        {
            LogCliFailure("GET /version", ex);

            return null;
        }
    }

    /// <summary>
    /// Check connectivity to the ArchLucid API (GET /health/live). Returns true if the API process responds successfully (liveness only).
    /// </summary>
    public async Task<bool> CheckHealthAsync(CancellationToken ct = default)
    {
        try
        {
            HttpResponseMessage response = await _http.GetAsync("/health/live", ct);

            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            LogCliFailure("Health check", ex);

            return false;
        }
    }

    /// <summary>
    /// GET a health path (e.g. <c>/health/ready</c>) and return HTTP status plus response body for operator diagnostics.
    /// </summary>
    public async Task<(int StatusCode, string Body)> GetHealthProbeAsync(string path, CancellationToken ct = default)
    {
        string normalized = path.StartsWith("/", StringComparison.Ordinal) ? path : "/" + path;

        try
        {
            HttpResponseMessage response = await _http.GetAsync(normalized, ct);
            string body = await response.Content.ReadAsStringAsync(ct);

            return ((int)response.StatusCode, body);
        }
        catch (Exception ex)
        {
            LogCliFailure($"GET {normalized}", ex);

            return (0, ex.Message);
        }
    }

    /// <summary>
    /// GET a path and capture at most <paramref name="maxBytes"/> of the UTF-8 body (for compact support-bundle probes).
    /// </summary>
    public async Task<(int StatusCode, string BodyPreview, bool BodyTruncated)> GetBoundedUtf8BodyAsync(
        string relativePath,
        int maxBytes,
        CancellationToken ct = default)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(maxBytes, 1);

        string normalized = relativePath.StartsWith("/", StringComparison.Ordinal)
            ? relativePath
            : "/" + relativePath;

        try
        {
            using HttpResponseMessage response = await _http.GetAsync(normalized, HttpCompletionOption.ResponseHeadersRead, ct);
            int code = (int)response.StatusCode;
            await using Stream stream = await response.Content.ReadAsStreamAsync(ct);
            using MemoryStream accumulator = new();

            byte[] buffer = new byte[8192];

            while (accumulator.Length < maxBytes + 1)
            {
                int toRead = (int)Math.Min(buffer.Length, maxBytes + 1 - accumulator.Length);


                if (toRead <= 0)
                    break;

                int n = await stream.ReadAsync(buffer.AsMemory(0, toRead), ct);


                if (n == 0)
                    break;

                accumulator.Write(buffer, 0, n);
            }

            byte[] all = accumulator.ToArray();
            bool truncated = all.Length > maxBytes;
            int useLen = truncated ? maxBytes : all.Length;
            string text = Encoding.UTF8.GetString(all, 0, useLen);

            return (code, text, truncated);
        }
        catch (Exception ex)
        {
            LogCliFailure($"GET {normalized} (bounded)", ex);

            return (0, ex.GetType().Name + ": " + ex.Message, false);
        }
    }

    /// <summary>
    /// Create an architecture run by submitting an ArchitectureRequest.
    /// </summary>
    public async Task<CreateRunResult> CreateRunAsync(ArchitectureRequest request, CancellationToken ct = default)
    {
        try
        {
            Gen.ArchitectureRequest? body = MapToGenerated(request);
            if (body is null)
            {
                return CreateRunResult.Fail(null, "Invalid architecture request payload.");
            }

            Gen.CreateArchitectureRunResponse created = await _api.RequestAsync(body, ct);
            CreateRunResponse? mapped = DeserializeRoundTrip<CreateRunResponse>(created);

            return CreateRunResult.Ok(mapped);
        }
        catch (Gen.ArchLucidApiException ex)
        {
            return CreateRunResult.Fail(ex.StatusCode, ResolveApiErrorMessage(ex));
        }
        catch (HttpRequestException ex)
        {
            return CreateRunResult.Fail(null, $"Cannot connect to ArchLucid API: {ex.Message}");
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
            Gen.AgentResult? genResult = MapToGenerated(result);
            if (genResult is null)
            {
                return new SubmitResultResult(false, null, "Invalid agent result payload.");
            }

            Gen.SubmitAgentResultRequest req = new()
            {
                Result = genResult
            };
            Gen.SubmitAgentResultResponse parsed = await _api.ResultAsync(runId, req, ct);

            return new SubmitResultResult(true, parsed.ResultId, null);
        }
        catch (Gen.ArchLucidApiException ex)
        {
            return new SubmitResultResult(false, null, ResolveApiErrorMessage(ex), ex.StatusCode);
        }
        catch (HttpRequestException ex)
        {
            return new SubmitResultResult(false, null, $"Cannot connect to ArchLucid API: {ex.Message}");
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
            Gen.RunDetailsResponse details = await _api.RunGETAsync(runId, ct);

            return DeserializeRoundTrip<GetRunResult>(details);
        }
        catch (Exception ex)
        {
            LogCliFailure($"GetRun({runId})", ex);

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
            Gen.CommitRunResponse result = await _api.CommitAsync(runId, ct);
            CommitRunResponse? mapped = DeserializeRoundTrip<CommitRunResponse>(result);

            return new CommitRunResult(true, mapped, null);
        }
        catch (Gen.ArchLucidApiException ex)
        {
            return new CommitRunResult(false, null, ResolveApiErrorMessage(ex), ex.StatusCode);
        }
        catch (HttpRequestException ex)
        {
            return new CommitRunResult(false, null, $"Cannot connect to ArchLucid API: {ex.Message}");
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
            Gen.SeedFakeResultsResponse result = await _api.SeedFakeResultsAsync(runId, ct);
            SeedFakeResultsResponse? mapped = DeserializeRoundTrip<SeedFakeResultsResponse>(result);

            return new SeedFakeResultsResult(true, mapped?.ResultCount ?? 0, null);
        }
        catch (Gen.ArchLucidApiException ex)
        {
            return new SeedFakeResultsResult(false, 0, ResolveApiErrorMessage(ex), ex.StatusCode);
        }
        catch (HttpRequestException ex)
        {
            return new SeedFakeResultsResult(false, 0, $"Cannot connect to ArchLucid API: {ex.Message}");
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
            Gen.GoldenManifest manifest = await _api.ManifestAsync(version, ct);

            return JsonSerializer.SerializeToElement(manifest, _jsonOptions);
        }
        catch (Exception ex)
        {
            LogCliFailure($"GetManifest({version})", ex);

            return null;
        }
    }

    public async Task<ComparisonHistoryResult?> SearchComparisonsAsync(
        string? comparisonType,
        string? leftRunId,
        string? rightRunId,
        string? leftExportRecordId,
        string? rightExportRecordId,
        string? label,
        string? tag,
        string? tags,
        string? sortBy,
        string? sortDir,
        string? cursor,
        int skip,
        int limit,
        CancellationToken ct = default)
    {
        try
        {
            string[]? tagsArray = string.IsNullOrWhiteSpace(tags)
                ? null
                : tags.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            Gen.ComparisonHistoryResponse history = await _api.ComparisonsGET4Async(
                comparisonType,
                leftRunId,
                rightRunId,
                leftExportRecordId,
                rightExportRecordId,
                label,
                createdFromUtc: null,
                createdToUtc: null,
                tag,
                tagsArray,
                sortBy,
                sortDir,
                cursor,
                skip,
                limit,
                ct);

            return DeserializeRoundTrip<ComparisonHistoryResult>(history);
        }
        catch (Exception ex)
        {
            LogCliFailure("GetComparisonHistory", ex);

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
            string uri =
                $"/v1/architecture/comparisons/{Uri.EscapeDataString(comparisonRecordId)}/replay?format={Uri.EscapeDataString(format)}";
            var body = new
            {
                format,
                replayMode,
                profile,
                persistReplay,
            };

            HttpResponseMessage response = await _http.PostAsJsonAsync(uri, body, _jsonOptions, ct);
            if (!response.IsSuccessStatusCode)
            {
                string contentError = await response.Content.ReadAsStringAsync(ct);
                Console.WriteLine($"Replay failed ({(int)response.StatusCode}): {contentError}");

                return false;
            }

            if (response.Headers.TryGetValues("X-ArchLucid-PersistedReplayRecordId", out IEnumerable<string>? persistedValues))
            {
                string? persistedId = persistedValues.FirstOrDefault();
                if (!string.IsNullOrWhiteSpace(persistedId))
                {
                    Console.WriteLine($"PersistedReplayRecordId: {persistedId}");
                }
            }

            string fileName = response.Content.Headers.ContentDisposition?.FileNameStar
                              ?? response.Content.Headers.ContentDisposition?.FileName
                              ?? $"comparison_{comparisonRecordId}.{format}";
            fileName = fileName.Trim('"');

            string targetPath = fileName;
            if (!string.IsNullOrWhiteSpace(outPath))
            {
                if (Directory.Exists(outPath) || outPath.EndsWith(Path.DirectorySeparatorChar) || outPath.EndsWith(Path.AltDirectorySeparatorChar))
                {
                    Directory.CreateDirectory(outPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
                    targetPath = Path.Combine(outPath, fileName);
                }
                else
                {
                    string? dir = Path.GetDirectoryName(outPath);
                    if (!string.IsNullOrWhiteSpace(dir))
                    {
                        Directory.CreateDirectory(dir);
                    }

                    targetPath = outPath;
                }
            }

            if (File.Exists(targetPath) && !force)
            {
                Console.WriteLine($"Refusing to overwrite existing file: {targetPath}");
                Console.WriteLine("Re-run with --force to overwrite, or choose a different --out path.");

                return false;
            }

            byte[] bytes = await response.Content.ReadAsByteArrayAsync(ct);
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

    public async Task<string?> GetComparisonDriftJsonAsync(string comparisonRecordId, CancellationToken ct = default)
    {
        try
        {
            Gen.DriftAnalysisResponse drift = await _api.DriftAsync(comparisonRecordId, ct);

            return JsonSerializer.Serialize(drift, _jsonOptions);
        }
        catch
        {
            return null;
        }
    }

    public async Task<string?> GetReplayDiagnosticsJsonAsync(int maxCount = 50, CancellationToken ct = default)
    {
        try
        {
            int safe = Math.Clamp(maxCount, 1, 100);
            Gen.ReplayDiagnosticsResponse diag = await _api.ReplayGETAsync(safe, ct);

            return JsonSerializer.Serialize(diag, _jsonOptions);
        }
        catch (Exception ex)
        {
            LogCliFailure("GetReplayDiagnosticsJson", ex);

            return null;
        }
    }

    public sealed class ComparisonSummary
    {
        public string ComparisonRecordId { get; set; } = string.Empty;
        public string ComparisonType { get; set; } = string.Empty;
        public string Format { get; set; } = string.Empty;
        public string Summary { get; set; } = string.Empty;
    }

    public async Task<ComparisonSummary?> GetComparisonSummaryAsync(string comparisonRecordId, CancellationToken ct = default)
    {
        try
        {
            Gen.ComparisonSummaryResponse summary = await _api.SummaryGET3Async(comparisonRecordId, ct);

            return DeserializeRoundTrip<ComparisonSummary>(summary);
        }
        catch (Exception ex)
        {
            LogCliFailure($"GetComparisonSummary({comparisonRecordId})", ex);

            return null;
        }
    }

    public async Task<bool> ReplayComparisonsBatchToZipAsync(
        IReadOnlyList<string> comparisonRecordIds,
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
            string uri = "/v1/architecture/comparisons/replay/batch";
            var body = new
            {
                comparisonRecordIds,
                format,
                replayMode,
                profile,
                persistReplay,
            };

            HttpResponseMessage response = await _http.PostAsJsonAsync(uri, body, _jsonOptions, ct);
            if (!response.IsSuccessStatusCode)
            {
                string contentError = await response.Content.ReadAsStringAsync(ct);
                Console.WriteLine($"Batch replay failed ({(int)response.StatusCode}): {contentError}");

                return false;
            }

            string fileName = response.Content.Headers.ContentDisposition?.FileNameStar
                              ?? response.Content.Headers.ContentDisposition?.FileName
                              ?? "comparison_replays.zip";
            fileName = fileName.Trim('"');

            string targetPath = fileName;
            if (!string.IsNullOrWhiteSpace(outPath))
            {
                if (Directory.Exists(outPath) || outPath.EndsWith(Path.DirectorySeparatorChar) || outPath.EndsWith(Path.AltDirectorySeparatorChar))
                {
                    Directory.CreateDirectory(outPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
                    targetPath = Path.Combine(outPath, fileName);
                }
                else
                {
                    string? dir = Path.GetDirectoryName(outPath);
                    if (!string.IsNullOrWhiteSpace(dir))
                    {
                        Directory.CreateDirectory(dir);
                    }

                    targetPath = outPath;
                }
            }

            if (File.Exists(targetPath) && !force)
            {
                Console.WriteLine($"Refusing to overwrite existing file: {targetPath}");
                Console.WriteLine("Re-run with --force to overwrite, or choose a different --out path.");

                return false;
            }

            byte[] bytes = await response.Content.ReadAsByteArrayAsync(ct);
            await File.WriteAllBytesAsync(targetPath, bytes, ct);
            Console.WriteLine($"Batch replay exported to {targetPath}");

            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Batch replay failed: {ex.Message}");

            return false;
        }
    }

    public sealed class DriftItem
    {
        public string Category { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
        public string? Description
        {
            get; set;
        }
    }

    public sealed class DriftAnalysis
    {
        public bool DriftDetected
        {
            get; set;
        }
        public string Summary { get; set; } = string.Empty;
        public List<DriftItem> Items { get; set; } = [];
    }

    public async Task<DriftAnalysis?> GetComparisonDriftAsync(string comparisonRecordId, CancellationToken ct = default)
    {
        try
        {
            Gen.DriftAnalysisResponse drift = await _api.DriftAsync(comparisonRecordId, ct);

            return DeserializeRoundTrip<DriftAnalysis>(drift);
        }
        catch (Exception ex)
        {
            LogCliFailure($"GetComparisonDrift({comparisonRecordId})", ex);

            return null;
        }
    }

    public sealed class ReplayDiagnostics
    {
        public List<ReplayDiagnosticsEntry> RecentReplays { get; set; } = [];
    }

    public sealed class ReplayDiagnosticsEntry
    {
        public DateTime TimestampUtc
        {
            get; set;
        }
        public string ComparisonRecordId { get; set; } = string.Empty;
        public string ComparisonType { get; set; } = string.Empty;
        public string Format { get; set; } = string.Empty;
        public string ReplayMode { get; set; } = string.Empty;
        public long DurationMs
        {
            get; set;
        }
        public bool Success
        {
            get; set;
        }
        public bool MetadataOnly
        {
            get; set;
        }
        public string? PersistedReplayRecordId
        {
            get; set;
        }
        public string? ErrorMessage
        {
            get; set;
        }
    }

    public async Task<ReplayDiagnostics?> GetReplayDiagnosticsAsync(int maxCount, CancellationToken ct = default)
    {
        try
        {
            Gen.ReplayDiagnosticsResponse diag = await _api.ReplayGETAsync(maxCount, ct);

            return DeserializeRoundTrip<ReplayDiagnostics>(diag);
        }
        catch (Exception ex)
        {
            LogCliFailure("GetReplayDiagnostics", ex);

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
            Gen.UpdateComparisonRecordRequest body = new()
            {
                Label = label,
                Tags = tags?.ToList(),
            };

            await _api.ComparisonsPATCHAsync(comparisonRecordId, body, ct);

            return true;
        }
        catch (Exception ex)
        {
            LogCliFailure($"UpdateComparisonRecord({comparisonRecordId})", ex);

            return false;
        }
    }

    public sealed class ComparisonHistoryResult
    {
        public List<ComparisonRecordSummary> Records { get; set; } = [];
        public string? NextCursor
        {
            get; set;
        }
    }

    public sealed class ComparisonRecordSummary
    {
        public string ComparisonRecordId { get; set; } = string.Empty;
        public string ComparisonType { get; set; } = string.Empty;
        public string? LeftRunId
        {
            get; set;
        }
        public string? RightRunId
        {
            get; set;
        }
        public string? LeftExportRecordId
        {
            get; set;
        }
        public string? RightExportRecordId
        {
            get; set;
        }
        public DateTime CreatedUtc
        {
            get; set;
        }
        public string? Label
        {
            get; set;
        }
        public List<string> Tags { get; set; } = [];
    }

    private TOut? DeserializeRoundTrip<TOut>(object value)
    {
        string json = JsonSerializer.Serialize(value, value.GetType(), _jsonOptions);

        return JsonSerializer.Deserialize<TOut>(json, _jsonOptions);
    }

    private Gen.ArchitectureRequest? MapToGenerated(ArchitectureRequest request)
    {
        string json = JsonSerializer.Serialize(request, _jsonOptions);

        return JsonSerializer.Deserialize<Gen.ArchitectureRequest>(json, _jsonOptions);
    }

    private Gen.AgentResult? MapToGenerated(AgentResult result)
    {
        string json = JsonSerializer.Serialize(result, _jsonOptions);

        return JsonSerializer.Deserialize<Gen.AgentResult>(json, _jsonOptions);
    }

    /// <summary>
    /// NSwag reads ProblemDetails from the stream with <c>ReadResponseAsString=false</c>, so <see cref="Gen.ArchLucidApiException.Response"/>
    /// is often empty even when <see cref="Gen.ArchLucidApiException{TResult}"/> carries a typed <see cref="Gen.ProblemDetails"/> body.
    /// </summary>
    private static string ResolveApiErrorMessage(Gen.ArchLucidApiException ex)
    {
        string? fromBody = TryParseError(ex.Response ?? string.Empty);

        if (!string.IsNullOrWhiteSpace(fromBody))
        {
            return fromBody;
        }

        if (ex is not Gen.ArchLucidApiException<Gen.ProblemDetails> typed)
            return ex.Message;

        if (!string.IsNullOrWhiteSpace(typed.Result?.Detail))
        {
            return typed.Result.Detail;
        }

        return !string.IsNullOrWhiteSpace(typed.Result?.Title) ? typed.Result.Title : ex.Message;
    }

    /// <summary>
    /// Parse error message from JSON. Supports RFC 9457 Problem Details (detail, title) and legacy (error, errors).
    /// </summary>
    private static string? TryParseError(string json)
    {
        try
        {
            JsonDocument doc = JsonDocument.Parse(json);
            JsonElement root = doc.RootElement;
            if (root.TryGetProperty("detail", out JsonElement detail))
            {
                return detail.GetString();
            }

            if (root.TryGetProperty("error", out JsonElement err))
            {
                return err.GetString();
            }

            if (root.TryGetProperty("errors", out JsonElement errs) && errs.ValueKind == JsonValueKind.Array)
            {
                return string.Join("; ", errs.EnumerateArray().Select(e => e.GetString()).Where(s => !string.IsNullOrEmpty(s)));
            }

            if (root.TryGetProperty("title", out JsonElement title))
            {
                return title.GetString();
            }
        }
        catch (Exception)
        {
            // Best-effort parse of arbitrary API error JSON; avoid stderr noise.
        }

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
        public int Status
        {
            get; set;
        }
        public DateTime CreatedUtc
        {
            get; set;
        }
        public DateTime? CompletedUtc
        {
            get; set;
        }
        public string? CurrentManifestVersion
        {
            get; set;
        }

        /// <summary>Persisted OpenTelemetry W3C trace id from run creation; null for older runs.</summary>
        public string? OtelTraceId
        {
            get; set;
        }
    }

    public sealed class AgentTaskInfo
    {
        public string TaskId { get; set; } = "";
        public string RunId { get; set; } = "";
        public int AgentType
        {
            get; set;
        }
        public string Objective { get; set; } = "";
        public int Status
        {
            get; set;
        }
    }

    public sealed class GetRunResult
    {
        public RunInfo Run { get; set; } = new();
        public List<AgentTaskInfo> Tasks { get; set; } = [];
        public List<object> Results { get; set; } = [];
    }

    public sealed record CommitRunResult(bool Success, CommitRunResponse? Response, string? Error, int? HttpStatusCode = null);

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

    public sealed record SeedFakeResultsResult(bool Success, int ResultCount, string? Error, int? HttpStatusCode = null);

    public sealed class SeedFakeResultsResponse
    {
        public string Message { get; set; } = "";
        public string RunId { get; set; } = "";
        public int ResultCount
        {
            get; set;
        }
    }

    public sealed record SubmitResultResult(bool Success, string? ResultId, string? Error, int? HttpStatusCode = null);

    public sealed class SubmitResultResponse
    {
        public string Message { get; set; } = "";
        public string RunId { get; set; } = "";
        public string ResultId { get; set; } = "";
    }
}
