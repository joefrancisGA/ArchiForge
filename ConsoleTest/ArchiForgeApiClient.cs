using System.Net.Http.Json;
using System.Text.Json;
using ArchiForge.Contracts.Requests;

namespace ArchiForge;

/// <summary>
/// HTTP client for the ArchiForge API.
/// </summary>
public sealed class ArchiForgeApiClient
{
    private readonly HttpClient _http;
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
    }

    public static string GetDefaultBaseUrl() =>
        Environment.GetEnvironmentVariable("ARCHIFORGE_API_URL") ?? "http://localhost:5128";

    /// <summary>
    /// Create an architecture run by submitting an ArchitectureRequest.
    /// </summary>
    public async Task<CreateRunResult> CreateRunAsync(ArchitectureRequest request, CancellationToken ct = default)
    {
        try
        {
            var response = await _http.PostAsJsonAsync("/v1/architecture/request", request, _jsonOptions, ct);
            var content = await response.Content.ReadAsStringAsync(ct);

            if (response.IsSuccessStatusCode)
            {
                var created = JsonSerializer.Deserialize<CreateRunResponse>(content, _jsonOptions);
                return CreateRunResult.Success(created);
            }

            var error = TryParseError(content);
            return CreateRunResult.Failure(response.StatusCode, error ?? content);
        }
        catch (HttpRequestException ex)
        {
            return CreateRunResult.Failure(null, $"Cannot connect to ArchiForge API: {ex.Message}");
        }
        catch (TaskCanceledException)
        {
            return CreateRunResult.Failure(null, "Request timed out.");
        }
    }

    /// <summary>
    /// Get run status, tasks, and results.
    /// </summary>
    public async Task<GetRunResult?> GetRunAsync(string runId, CancellationToken ct = default)
    {
        try
        {
            var response = await _http.GetAsync($"/v1/architecture/run/{Uri.EscapeDataString(runId)}", ct);
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
            var response = await _http.PostAsync($"/v1/architecture/run/{Uri.EscapeDataString(runId)}/commit", null, ct);
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
    /// Get manifest by version.
    /// </summary>
    public async Task<object?> GetManifestAsync(string version, CancellationToken ct = default)
    {
        try
        {
            var response = await _http.GetAsync($"/v1/architecture/manifest/{Uri.EscapeDataString(version)}", ct);
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

    private static string? TryParseError(string json)
    {
        try
        {
            var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("error", out var err))
                return err.GetString();
            if (doc.RootElement.TryGetProperty("errors", out var errs) && errs.ValueKind == JsonValueKind.Array)
                return string.Join("; ", errs.EnumerateArray().Select(e => e.GetString()).Where(s => !string.IsNullOrEmpty(s)));
        }
        catch { }
        return null;
    }

    public sealed record CreateRunResult(bool Success, CreateRunResponse? Response, string? Error, int? StatusCode)
    {
        public static CreateRunResult Success(CreateRunResponse? r) => new(true, r, null, null);
        public static CreateRunResult Failure(int? statusCode, string error) => new(false, null, error, statusCode);
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
}
