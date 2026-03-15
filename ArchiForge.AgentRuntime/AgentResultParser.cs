using System.Text.Json;
using ArchiForge.Contracts.Agents;

namespace ArchiForge.AgentRuntime;

public sealed class AgentResultParser : IAgentResultParser
{
    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    public AgentResult ParseAndValidate(
        string json,
        string runId,
        string taskId)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            throw new InvalidOperationException("Agent returned empty JSON.");
        }

        var result = JsonSerializer.Deserialize<AgentResult>(json, _jsonOptions);

        if (result is null)
        {
            throw new InvalidOperationException("Failed to deserialize AgentResult.");
        }

        if (!string.Equals(result.RunId, runId, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"AgentResult.RunId '{result.RunId}' does not match expected runId '{runId}'.");
        }

        if (!string.Equals(result.TaskId, taskId, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"AgentResult.TaskId '{result.TaskId}' does not match expected taskId '{taskId}'.");
        }

        if (result.Confidence < 0.0 || result.Confidence > 1.0)
        {
            throw new InvalidOperationException("AgentResult.Confidence must be between 0 and 1.");
        }

        return result;
    }
}
