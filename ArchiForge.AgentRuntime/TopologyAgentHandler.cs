using System.Text;
using ArchiForge.Contracts.Agents;
using ArchiForge.Contracts.Common;
using ArchiForge.Contracts.Requests;

namespace ArchiForge.AgentRuntime;

public sealed class TopologyAgentHandler : IAgentHandler
{
    private readonly IAgentCompletionClient _completionClient;
    private readonly IAgentResultParser _resultParser;

    public TopologyAgentHandler(
        IAgentCompletionClient completionClient,
        IAgentResultParser resultParser)
    {
        _completionClient = completionClient;
        _resultParser = resultParser;
    }

    public AgentType AgentType => AgentType.Topology;

    public async Task<AgentResult> ExecuteAsync(
        string runId,
        ArchitectureRequest request,
        AgentTask task,
        CancellationToken cancellationToken = default)
    {
        var systemPrompt = """
            You are the ArchiForge Topology Agent.
            Return only valid JSON for an AgentResult.
            Do not include markdown.
            Do not include commentary outside JSON.
            Your responsibility is to propose services, datastores, and relationships.
            """;

        var userPrompt = BuildPrompt(request, task);

        var rawJson = await _completionClient.CompleteJsonAsync(
            systemPrompt,
            userPrompt,
            runId,
            task.TaskId,
            cancellationToken);

        return _resultParser.ParseAndValidate(rawJson, runId, task.TaskId);
    }

    private static string BuildPrompt(ArchitectureRequest request, AgentTask task)
    {
        var sb = new StringBuilder();

        sb.AppendLine($"SystemName: {request.SystemName}");
        sb.AppendLine($"Environment: {request.Environment}");
        sb.AppendLine($"Description: {request.Description}");

        if (request.Constraints.Count > 0)
        {
            sb.AppendLine("Constraints:");
            foreach (var constraint in request.Constraints)
            {
                sb.AppendLine($"- {constraint}");
            }
        }

        if (request.RequiredCapabilities.Count > 0)
        {
            sb.AppendLine("RequiredCapabilities:");
            foreach (var capability in request.RequiredCapabilities)
            {
                sb.AppendLine($"- {capability}");
            }
        }

        sb.AppendLine($"Objective: {task.Objective}");

        return sb.ToString();
    }
}
