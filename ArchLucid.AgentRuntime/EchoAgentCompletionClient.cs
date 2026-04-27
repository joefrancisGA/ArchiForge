using System.Text.Json;
using System.Text.Json.Serialization;

using ArchLucid.AgentSimulator.Services;
using ArchLucid.Contracts.Abstractions.Agents;
using ArchLucid.Contracts.Agents;
using ArchLucid.Contracts.Common;
using ArchLucid.Contracts.Requests;

namespace ArchLucid.AgentRuntime;

/// <summary>
///     Non-network <see cref="IAgentCompletionClient" /> that returns deterministic <see cref="AgentResult" /> JSON (via
///     <see cref="FakeScenarioFactory" />)
///     so Real-mode pipelines exercise the same code path as Azure OpenAI without outbound LLM calls. Telemetry labels
///     should be set to <c>echo</c>.
/// </summary>
public sealed class EchoAgentCompletionClient : IAgentCompletionClient
{
    private static readonly LlmProviderDescriptor EchoDescriptor =
        LlmProviderDescriptor.ForOffline("echo", "echo");

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true, Converters = { new JsonStringEnumConverter() }
    };

    /// <inheritdoc />
    public LlmProviderDescriptor Descriptor => EchoDescriptor;

    /// <inheritdoc />
    public Task<string> CompleteJsonAsync(
        string systemPrompt,
        string userPrompt,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(systemPrompt);
        ArgumentNullException.ThrowIfNull(userPrompt);

        (string runId, string taskId) = ParseRunAndTaskIds(userPrompt);
        ArchitectureRequest request = BuildDefaultRequest();
        AgentType agentKind = ResolveAgentType(systemPrompt);

        AgentResult result = agentKind switch
        {
            AgentType.Compliance => FakeScenarioFactory.CreateComplianceResult(runId, taskId, request),
            AgentType.Critic => FakeScenarioFactory.CreateCriticResult(runId, taskId, request),
            _ => FakeScenarioFactory.CreateTopologyResult(runId, taskId, request)
        };

        string json = JsonSerializer.Serialize(result, JsonOptions);

        return Task.FromResult(json);
    }

    private static AgentType ResolveAgentType(string systemPrompt)
    {
        if (systemPrompt.Contains("Compliance Agent", StringComparison.Ordinal))
            return AgentType.Compliance;


        return systemPrompt.Contains("Critic Agent", StringComparison.Ordinal) ? AgentType.Critic : AgentType.Topology;
    }

    private static ArchitectureRequest BuildDefaultRequest()
    {
        return new ArchitectureRequest
        {
            SystemName = "Echo", Description = "Echo completion client (no LLM).", Environment = "prod"
        };
    }

    private static (string RunId, string TaskId) ParseRunAndTaskIds(string userPrompt)
    {
        string runId = "RUN-001";
        string taskId = "TASK-TOPO-001";

        foreach (string line in userPrompt.Split('\n'))
        {
            ReadOnlySpan<char> span = line.AsSpan().Trim();

            if (span.StartsWith("RunId:", StringComparison.OrdinalIgnoreCase))

                runId = span.Length > 6 ? span[6..].Trim().ToString() : runId;

            else if (span.StartsWith("TaskId:", StringComparison.OrdinalIgnoreCase))

                taskId = span.Length > 7 ? span[7..].Trim().ToString() : taskId;
        }

        return (runId, taskId);
    }
}
