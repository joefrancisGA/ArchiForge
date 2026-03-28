using System.Text.Json;
using System.Text.Json.Serialization;

using ArchiForge.Contracts.Agents;
using ArchiForge.Contracts.Common;

namespace ArchiForge.AgentRuntime;

public sealed class AgentResultParser : IAgentResultParser
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true,
        // Agent/LLM payloads use string enum names (see schemas/agentresult.schema.json); default is numeric.
        Converters = { new JsonStringEnumConverter() }
    };

    public AgentResult ParseAndValidate(
        string json,
        string expectedRunId,
        string expectedTaskId,
        AgentType expectedAgentType)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            throw new InvalidOperationException("Agent returned empty JSON.");
        }

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
        {
            throw new InvalidOperationException("Agent returned null AgentResult.");
        }

        if (!string.Equals(result.RunId, expectedRunId, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"AgentResult.RunId '{result.RunId}' does not match expected runId '{expectedRunId}'.");
        }

        if (!string.Equals(result.TaskId, expectedTaskId, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"AgentResult.TaskId '{result.TaskId}' does not match expected taskId '{expectedTaskId}'.");
        }

        if (result.AgentType != expectedAgentType)
        {
            throw new InvalidOperationException(
                $"AgentResult.AgentType '{result.AgentType}' does not match expected type '{expectedAgentType}'.");
        }

        if (string.IsNullOrWhiteSpace(result.ResultId))
        {
            throw new InvalidOperationException("AgentResult.ResultId is required.");
        }

        if (result.Claims is null)
        {
            throw new InvalidOperationException("AgentResult.Claims is required.");
        }

        if (result.EvidenceRefs is null)
        {
            throw new InvalidOperationException("AgentResult.EvidenceRefs is required.");
        }

        if (result.Confidence < 0.0 || result.Confidence > 1.0)
        {
            throw new InvalidOperationException("AgentResult.Confidence must be between 0 and 1.");
        }

        return result;
    }
}
