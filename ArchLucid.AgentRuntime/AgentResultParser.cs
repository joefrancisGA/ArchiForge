using System.Text.Json;
using System.Text.Json.Serialization;

using ArchLucid.Contracts.Agents;
using ArchLucid.Contracts.Common;
using ArchLucid.Core.Diagnostics;
using ArchLucid.Decisioning.Validation;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ArchLucid.AgentRuntime;

/// <summary>
/// JSON deserializer for <see cref="AgentResult"/> using web defaults and string enums; validates JSON schema (when configured), then ids and agent type.
/// </summary>
public sealed class AgentResultParser : IAgentResultParser
{
    private const int MaxTruncatedJsonLength = 2000;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() },
    };

    private readonly ISchemaValidationService _schemaValidationService;
    private readonly IOptions<AgentResultSchemaValidationOptions> _options;
    private readonly ILogger<AgentResultParser> _logger;

    /// <summary>Uses passthrough schema validation (always valid) and default options — for tests and simple construction.</summary>
    public AgentResultParser()
        : this(
            new PassthroughSchemaValidationService(),
            Options.Create(new AgentResultSchemaValidationOptions()),
            Microsoft.Extensions.Logging.Abstractions.NullLogger<AgentResultParser>.Instance)
    {
    }

    /// <summary>Production constructor.</summary>
    public AgentResultParser(
        ISchemaValidationService schemaValidationService,
        IOptions<AgentResultSchemaValidationOptions> options,
        ILogger<AgentResultParser> logger)
    {
        _schemaValidationService = schemaValidationService ?? throw new ArgumentNullException(nameof(schemaValidationService));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public AgentResult ParseAndValidate(
        string json,
        string expectedRunId,
        string expectedTaskId,
        AgentType expectedAgentType)
    {
        if (string.IsNullOrWhiteSpace(json))

            throw new InvalidOperationException("Agent returned empty JSON.");


        AgentResult? result;

        try
        {
            result = JsonSerializer.Deserialize<AgentResult>(json, JsonOptions);
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException("Failed to deserialize AgentResult JSON.", ex);
        }
        catch (NotSupportedException ex)
        {
            throw new InvalidOperationException("AgentResult JSON contains an unsupported type mapping.", ex);
        }

        if (result is null)

            throw new InvalidOperationException("Agent returned null AgentResult.");


        SchemaValidationResult schemaResult = _schemaValidationService.ValidateAgentResultJson(json);
        string agentTypeLabel = expectedAgentType.ToString();

        if (!schemaResult.IsValid)
        {
            ArchLucidInstrumentation.RecordAgentResultSchemaValidation(agentTypeLabel, "invalid");

            if (_options.Value.EnforceOnParse)
            {
                string truncated = TruncateJson(json);

                throw new AgentResultSchemaViolationException(
                    $"AgentResult JSON failed schema validation ({schemaResult.Errors.Count} error(s)).",
                    schemaResult.Errors,
                    truncated,
                    expectedAgentType);
            }

            if (_logger.IsEnabled(LogLevel.Warning))
            {
                _logger.LogWarning(
                    "AgentResult JSON failed schema validation but AgentExecution:SchemaValidation:EnforceOnParse is false; continuing. Errors: {Errors}",
                    string.Join("; ", schemaResult.Errors));
            }
        }
        else
        {
            ArchLucidInstrumentation.RecordAgentResultSchemaValidation(agentTypeLabel, "valid");
        }

        if (!string.Equals(result.RunId, expectedRunId, StringComparison.OrdinalIgnoreCase))

            throw new InvalidOperationException(
                $"AgentResult.RunId '{result.RunId}' does not match expected runId '{expectedRunId}'.");


        if (!string.Equals(result.TaskId, expectedTaskId, StringComparison.OrdinalIgnoreCase))

            throw new InvalidOperationException(
                $"AgentResult.TaskId '{result.TaskId}' does not match expected taskId '{expectedTaskId}'.");


        if (result.AgentType != expectedAgentType)

            throw new InvalidOperationException(
                $"AgentResult.AgentType '{result.AgentType}' does not match expected type '{expectedAgentType}'.");


        if (string.IsNullOrWhiteSpace(result.ResultId))

            throw new InvalidOperationException("AgentResult.ResultId is required.");


        if (result.Claims is null)

            throw new InvalidOperationException("AgentResult.Claims is required.");


        if (result.EvidenceRefs is null)

            throw new InvalidOperationException("AgentResult.EvidenceRefs is required.");


        if (result.Confidence < 0.0 || result.Confidence > 1.0)

            throw new InvalidOperationException("AgentResult.Confidence must be between 0 and 1.");


        return result;
    }

    private static string TruncateJson(string json)
    {
        if (json.Length <= MaxTruncatedJsonLength)

            return json;


        return json[..MaxTruncatedJsonLength] + "…";
    }
}
