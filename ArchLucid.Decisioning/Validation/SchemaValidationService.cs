using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

using Json.Schema;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ArchLucid.Decisioning.Validation;

public sealed class SchemaValidationService : ISchemaValidationService
{
    /// <summary>OpenTelemetry meter name for schema validation metrics.</summary>
    public const string MeterName = "ArchLucid.Decisioning.SchemaValidation";

    private static readonly Meter SMeter = new(MeterName, "1.0");

    /// <summary>Counts total validation calls by schema name and outcome (valid/invalid).</summary>
    private static readonly Counter<long> SValidationCounter =
        SMeter.CreateCounter<long>("schema_validation_total", description: "Total schema validation calls.");

    /// <summary>Records validation duration in milliseconds by schema name.</summary>
    private static readonly Histogram<double> SValidationDurationMs =
        SMeter.CreateHistogram<double>("schema_validation_duration_ms", "ms", "Schema validation duration.");

    private readonly Lazy<JsonSchema> _agentResultSchema;
    private readonly Lazy<JsonSchema> _comparisonExplanationSchema;
    private readonly Lazy<JsonSchema> _explanationRunSchema;
    private readonly Lazy<JsonSchema> _goldenManifestSchema;

    private readonly ILogger<SchemaValidationService> _logger;
    private readonly SchemaValidationOptions _options;

    /// <summary>
    ///     Optional LRU-style result cache keyed by SHA-256(json).
    ///     Cleared when it reaches <see cref="SchemaValidationOptions.ResultCacheMaxSize" /> to bound memory.
    /// </summary>
    private readonly ConcurrentDictionary<string, SchemaValidationResult>? _resultCache;

    public SchemaValidationService(
        ILogger<SchemaValidationService> logger,
        IOptions<SchemaValidationOptions>? options)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));

        _agentResultSchema = new Lazy<JsonSchema>(() =>
            LoadSchema(_options.AgentResultSchemaPath, "AgentResult"));
        _goldenManifestSchema = new Lazy<JsonSchema>(() =>
            LoadSchema(_options.GoldenManifestSchemaPath, "GoldenManifest"));
        _explanationRunSchema = new Lazy<JsonSchema>(() =>
            LoadSchema(_options.ExplanationRunSchemaPath, "ExplanationRun"));
        _comparisonExplanationSchema = new Lazy<JsonSchema>(() =>
            LoadSchema(_options.ComparisonExplanationSchemaPath, "ComparisonExplanation"));

        if (_options.EnableResultCaching)

            _resultCache = new ConcurrentDictionary<string, SchemaValidationResult>(StringComparer.Ordinal);
    }

    public SchemaValidationResult ValidateAgentResultJson(string json)
    {
        return Validate(json, _agentResultSchema.Value, "AgentResult");
    }

    public SchemaValidationResult ValidateGoldenManifestJson(string json)
    {
        return Validate(json, _goldenManifestSchema.Value, "GoldenManifest");
    }

    public SchemaValidationResult ValidateExplanationRunJson(string json)
    {
        return Validate(json, _explanationRunSchema.Value, "ExplanationRun");
    }

    public SchemaValidationResult ValidateComparisonExplanationJson(string json)
    {
        return Validate(json, _comparisonExplanationSchema.Value, "ComparisonExplanation");
    }

    public Task<SchemaValidationResult> ValidateAgentResultJsonAsync(
        string json,
        CancellationToken cancellationToken = default)
    {
        return ValidateAsync(json, _agentResultSchema.Value, "AgentResult", cancellationToken);
    }

    public Task<SchemaValidationResult> ValidateGoldenManifestJsonAsync(
        string json,
        CancellationToken cancellationToken = default)
    {
        return ValidateAsync(json, _goldenManifestSchema.Value, "GoldenManifest", cancellationToken);
    }

    private JsonSchema LoadSchema(string relativePath, string schemaName)
    {
        try
        {
            string fullPath = Path.Combine(AppContext.BaseDirectory, relativePath);

            if (!File.Exists(fullPath))
            {

                if (_logger.IsEnabled(LogLevel.Error))

                    _logger.LogError("Schema file not found: {FullPath} for {SchemaName}", fullPath, schemaName);

                throw new FileNotFoundException($"Schema file not found: {fullPath}", fullPath);
            }

            if (_logger.IsEnabled(LogLevel.Information))

                _logger.LogInformation("Loading schema {SchemaName} from {FullPath}", schemaName, fullPath);

            string schemaText = File.ReadAllText(fullPath);
            JsonSchema schema = JsonSchema.FromText(schemaText);

            if (_logger.IsEnabled(LogLevel.Information))

                _logger.LogInformation("Successfully loaded schema {SchemaName}", schemaName);

            return schema;
        }
        catch (Exception ex) when (ex is not FileNotFoundException)
        {

            if (_logger.IsEnabled(LogLevel.Error))

                _logger.LogError(ex, "Failed to load or parse schema {SchemaName} from {RelativePath}", schemaName,
                    relativePath);

            throw;
        }
    }

    private SchemaValidationResult Validate(
        string json,
        JsonSchema schema,
        string objectName)
    {
        if (_resultCache is null)
            return ValidateCore(json, schema, objectName);

        string cacheKey = ComputeHash(objectName, json);

        if (_resultCache.TryGetValue(cacheKey, out SchemaValidationResult? cached))
            return cached;

        SchemaValidationResult fresh = ValidateCore(json, schema, objectName);
        AddToCache(cacheKey, fresh);
        return fresh;
    }

    private SchemaValidationResult ValidateCore(
        string json,
        JsonSchema schema,
        string objectName)
    {
        Stopwatch sw = Stopwatch.StartNew();
        SchemaValidationResult result = new();

        if (string.IsNullOrWhiteSpace(json))
        {
            string error = $"{objectName} JSON payload is empty.";
            result.Errors.Add(error);

            if (_logger.IsEnabled(LogLevel.Warning))

                _logger.LogWarning("Validation failed for {ObjectName}: Empty payload", objectName);

            EmitMetrics(objectName, false, sw.Elapsed.TotalMilliseconds);
            return result;
        }

        JsonDocument doc;
        try
        {
            doc = JsonDocument.Parse(json);
        }
        catch (JsonException ex)
        {
            string error = $"{objectName} JSON could not be parsed: {ex.Message}";
            result.Errors.Add(error);

            if (_logger.IsEnabled(LogLevel.Warning))

                _logger.LogWarning(ex, "Validation failed for {ObjectName}: Invalid JSON", objectName);

            EmitMetrics(objectName, false, sw.Elapsed.TotalMilliseconds);
            return result;
        }

        using (doc)
        {
            EvaluationResults evaluation = schema.Evaluate(
                doc.RootElement,
                new EvaluationOptions { OutputFormat = OutputFormat.List });

            if (evaluation.IsValid)
            {

                if (_logger.IsEnabled(LogLevel.Debug))

                    _logger.LogDebug("Validation succeeded for {ObjectName}", objectName);

                EmitMetrics(objectName, true, sw.Elapsed.TotalMilliseconds);
                return result;
            }

            CollectErrors(evaluation, result, objectName);

            if (_logger.IsEnabled(LogLevel.Warning))

                _logger.LogWarning(
                    "Validation failed for {ObjectName} with {ErrorCount} errors",
                    objectName,
                    result.Errors.Count);

            EmitMetrics(objectName, false, sw.Elapsed.TotalMilliseconds);
            return result;
        }
    }

    private void AddToCache(string key, SchemaValidationResult result)
    {
        if (_resultCache is null)
            return;

        if (_resultCache.Count >= _options.ResultCacheMaxSize)

            _resultCache.Clear();

        _resultCache.TryAdd(key, result);
    }

    /// <summary>Computes a SHA-256 hash over schemaName + json to use as a cache key.</summary>
    private static string ComputeHash(string schemaName, string json)
    {
        byte[] bytes = SHA256.HashData(Encoding.UTF8.GetBytes(schemaName + "|" + json));
        return Convert.ToHexString(bytes);
    }

    private static void EmitMetrics(string objectName, bool valid, double elapsedMs)
    {
        TagList tags = new() { { "schema", objectName }, { "outcome", valid ? "valid" : "invalid" } };

        SValidationCounter.Add(1, tags);
        SValidationDurationMs.Record(elapsedMs, new KeyValuePair<string, object?>("schema", objectName));
    }

    private Task<SchemaValidationResult> ValidateAsync(
        string json,
        JsonSchema schema,
        string objectName,
        CancellationToken cancellationToken)
    {
        return Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Validate(json, schema, objectName);
        }, cancellationToken);
    }

    private void CollectErrors(
        EvaluationResults evaluation,
        SchemaValidationResult result,
        string objectName)
    {
        if (evaluation.Errors is not null && evaluation.Errors.Count > 0)

            foreach (KeyValuePair<string, string> kvp in evaluation.Errors)
            {
                string message = kvp.Value;
                string location = evaluation.InstanceLocation.ToString();

                if (string.IsNullOrEmpty(location))
                    location = "(root)";
                string? schemaPath = evaluation.SchemaLocation?.ToString();
                string keyword = kvp.Key;

                string errorMessage = $"{objectName} schema error at '{location}': {message}";
                result.Errors.Add(errorMessage);

                if (_options.EnableDetailedErrors)

                    result.DetailedErrors.Add(new SchemaValidationError { Message = message, Location = location, SchemaPath = schemaPath, Keyword = keyword });
            }

        if (evaluation.Details is null)
            return;

        foreach (EvaluationResults detail in evaluation.Details)

            CollectErrors(detail, result, objectName);
    }
}

