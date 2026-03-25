using System.Text.Json;

using Json.Schema;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ArchiForge.DecisionEngine.Validation;

public sealed class SchemaValidationService : ISchemaValidationService
{
    private readonly ILogger<SchemaValidationService> _logger;
    private readonly SchemaValidationOptions _options;
    private readonly Lazy<JsonSchema> _agentResultSchema;
    private readonly Lazy<JsonSchema> _goldenManifestSchema;

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
    }

    public SchemaValidationResult ValidateAgentResultJson(string json)
    {
        return Validate(json, _agentResultSchema.Value, "AgentResult");
    }

    public SchemaValidationResult ValidateGoldenManifestJson(string json)
    {
        return Validate(json, _goldenManifestSchema.Value, "GoldenManifest");
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
                {
                    _logger.LogError("Schema file not found: {FullPath} for {SchemaName}", fullPath, schemaName);
                }
                throw new FileNotFoundException($"Schema file not found: {fullPath}", fullPath);
            }

            if (_logger.IsEnabled(LogLevel.Information))
            {
                _logger.LogInformation("Loading schema {SchemaName} from {FullPath}", schemaName, fullPath);
            }
            string schemaText = File.ReadAllText(fullPath);
            JsonSchema schema = JsonSchema.FromText(schemaText);

            if (_logger.IsEnabled(LogLevel.Information))
            {
                _logger.LogInformation("Successfully loaded schema {SchemaName}", schemaName);
            }
            return schema;
        }
        catch (Exception ex) when (ex is not FileNotFoundException)
        {
            if (_logger.IsEnabled(LogLevel.Error))
            {
                _logger.LogError(ex, "Failed to load or parse schema {SchemaName} from {RelativePath}", schemaName, relativePath);
            }
            throw;
        }
    }

    private SchemaValidationResult Validate(
        string json,
        JsonSchema schema,
        string objectName)
    {
        SchemaValidationResult result = new SchemaValidationResult();

        if (string.IsNullOrWhiteSpace(json))
        {
            string error = $"{objectName} JSON payload is empty.";
            result.Errors.Add(error);
            if (_logger.IsEnabled(LogLevel.Warning))
            {
                _logger.LogWarning("Validation failed for {ObjectName}: Empty payload", objectName);
            }
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
            {
                _logger.LogWarning(ex, "Validation failed for {ObjectName}: Invalid JSON", objectName);
            }
            return result;
        }

        using (doc)
        {
            EvaluationResults evaluation = schema.Evaluate(
                doc.RootElement,
                new EvaluationOptions
                {
                    OutputFormat = OutputFormat.List
                });

            if (evaluation.IsValid)
            {
                if (_logger.IsEnabled(LogLevel.Debug))
                {
                    _logger.LogDebug("Validation succeeded for {ObjectName}", objectName);
                }
                return result;
            }

            CollectErrors(evaluation, result, objectName);
            if (_logger.IsEnabled(LogLevel.Warning))
            {
                _logger.LogWarning(
                    "Validation failed for {ObjectName} with {ErrorCount} errors",
                    objectName,
                    result.Errors.Count);
            }

            return result;
        }
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
        {
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
                {
                    result.DetailedErrors.Add(new SchemaValidationError
                    {
                        Message = message,
                        Location = location,
                        SchemaPath = schemaPath,
                        Keyword = keyword
                    });
                }
            }
        }

        if (evaluation.Details is null)
        {
            return;
        }

        foreach (EvaluationResults detail in evaluation.Details)
        {
            CollectErrors(detail, result, objectName);
        }
    }
}

