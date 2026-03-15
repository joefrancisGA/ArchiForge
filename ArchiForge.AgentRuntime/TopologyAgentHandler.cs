using System.Text;
using System.Text.Json;
using ArchiForge.Contracts.Agents;
using ArchiForge.Contracts.Common;
using ArchiForge.Contracts.Requests;

namespace ArchiForge.AgentRuntime;

public sealed class TopologyAgentHandler : IAgentHandler
{
    private static readonly JsonSerializerOptions TraceJsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    private readonly IAgentCompletionClient _completionClient;
    private readonly IAgentResultParser _resultParser;
    private readonly IAgentExecutionTraceRecorder _traceRecorder;

    public TopologyAgentHandler(
        IAgentCompletionClient completionClient,
        IAgentResultParser resultParser,
        IAgentExecutionTraceRecorder traceRecorder)
    {
        _completionClient = completionClient;
        _resultParser = resultParser;
        _traceRecorder = traceRecorder;
    }

    public AgentType AgentType => AgentType.Topology;

    public async Task<AgentResult> ExecuteAsync(
        string runId,
        ArchitectureRequest request,
        AgentEvidencePackage evidence,
        AgentTask task,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(evidence);
        ArgumentNullException.ThrowIfNull(task);

        var systemPrompt = BuildSystemPrompt();
        var userPrompt = BuildUserPrompt(runId, request, evidence, task);

        string rawJson = string.Empty;

        try
        {
            rawJson = await _completionClient.CompleteJsonAsync(
                systemPrompt,
                userPrompt,
                cancellationToken);

            var parsed = _resultParser.ParseAndValidate(
                rawJson,
                expectedRunId: runId,
                expectedTaskId: task.TaskId,
                expectedAgentType: AgentType.Topology);

            var parsedJson = JsonSerializer.Serialize(parsed, TraceJsonOptions);

            await _traceRecorder.RecordAsync(
                runId,
                task.TaskId,
                AgentType.Topology,
                systemPrompt,
                userPrompt,
                rawJson,
                parsedJson,
                parseSucceeded: true,
                errorMessage: null,
                cancellationToken: cancellationToken);

            return parsed;
        }
        catch (Exception ex)
        {
            await _traceRecorder.RecordAsync(
                runId,
                task.TaskId,
                AgentType.Topology,
                systemPrompt,
                userPrompt,
                rawJson,
                parsedResultJson: null,
                parseSucceeded: false,
                errorMessage: ex.Message,
                cancellationToken: cancellationToken);

            throw;
        }
    }

    private static string BuildSystemPrompt()
    {
        return """
You are the ArchiForge Topology Agent.

Your job is to propose only topology-related architecture structure for an Azure-based system.

You must return ONLY valid JSON that can be deserialized into an AgentResult object.

Do not include markdown.
Do not include commentary outside JSON.
Do not wrap the response in code fences.

Rules:
1. AgentType must be "Topology".
2. RunId and TaskId must exactly match the values provided by the user prompt.
3. Confidence must be between 0.0 and 1.0.
4. ProposedChanges may include only:
   - AddedServices
   - AddedDatastores
   - AddedRelationships
   - Warnings
5. Do not add compliance controls unless they are structurally inseparable from the topology.
6. Do not produce cost estimates.
7. Prefer managed Azure services for an MVP unless the request clearly requires otherwise.
8. Keep the topology simple, coherent, and production-reasonable.

Use these enum string values exactly where needed:

AgentType:
- Topology

ServiceType:
- Api
- Worker
- Ui
- Integration
- DataService
- SearchService
- AiService

RuntimePlatform:
- AppService
- Functions
- Aks
- Vm
- ContainerApps
- SqlServer
- AzureAiSearch
- AzureOpenAi
- Redis
- BlobStorage
- KeyVault

DatastoreType:
- Sql
- NoSql
- Object
- Cache
- Search

RelationshipType:
- Calls
- ReadsFrom
- WritesTo
- PublishesTo
- SubscribesTo
- AuthenticatesWith

Return JSON matching this conceptual shape:

{
  "resultId": "string",
  "taskId": "string",
  "runId": "string",
  "agentType": "Topology",
  "claims": ["string"],
  "evidenceRefs": ["string"],
  "confidence": 0.0,
  "findings": [
    {
      "findingId": "string",
      "sourceAgent": "Topology",
      "severity": "Info",
      "category": "Topology",
      "message": "string",
      "evidenceRefs": ["string"]
    }
  ],
  "proposedChanges": {
    "proposalId": "string",
    "sourceAgent": "Topology",
    "addedServices": [],
    "addedDatastores": [],
    "addedRelationships": [],
    "requiredControls": [],
    "warnings": ["string"]
  },
  "createdUtc": "2026-03-15T14:00:00Z"
}
""";
    }

    private static string BuildUserPrompt(
        string runId,
        ArchitectureRequest request,
        AgentEvidencePackage evidence,
        AgentTask task)
    {
        var sb = new StringBuilder();

        sb.AppendLine("Generate a topology AgentResult.");
        sb.AppendLine();

        sb.AppendLine($"RunId: {runId}");
        sb.AppendLine($"TaskId: {task.TaskId}");
        sb.AppendLine("AgentType: Topology");
        sb.AppendLine();

        sb.AppendLine("Architecture Request");
        sb.AppendLine($"RequestId: {request.RequestId}");
        sb.AppendLine($"SystemName: {request.SystemName}");
        sb.AppendLine($"Environment: {request.Environment}");
        sb.AppendLine($"CloudProvider: {request.CloudProvider}");
        sb.AppendLine($"Description: {request.Description}");
        sb.AppendLine();

        if (request.Constraints.Count > 0)
        {
            sb.AppendLine("Constraints:");
            foreach (var constraint in request.Constraints)
            {
                sb.AppendLine($"- {constraint}");
            }

            sb.AppendLine();
        }

        if (request.RequiredCapabilities.Count > 0)
        {
            sb.AppendLine("Required Capabilities:");
            foreach (var capability in request.RequiredCapabilities)
            {
                sb.AppendLine($"- {capability}");
            }

            sb.AppendLine();
        }

        if (request.Assumptions.Count > 0)
        {
            sb.AppendLine("Assumptions:");
            foreach (var assumption in request.Assumptions)
            {
                sb.AppendLine($"- {assumption}");
            }

            sb.AppendLine();
        }

        sb.AppendLine("Evidence Package");
        sb.AppendLine($"EvidencePackageId: {evidence.EvidencePackageId}");
        sb.AppendLine();

        if (evidence.Policies.Count > 0)
        {
            sb.AppendLine("Policies:");
            foreach (var policy in evidence.Policies)
            {
                sb.AppendLine($"- {policy.Title}: {policy.Summary}");
                if (policy.RequiredControls.Count > 0)
                {
                    sb.AppendLine($"  RequiredControls: {string.Join(", ", policy.RequiredControls)}");
                }
            }

            sb.AppendLine();
        }

        if (evidence.ServiceCatalog.Count > 0)
        {
            sb.AppendLine("Service Catalog Hints:");
            foreach (var service in evidence.ServiceCatalog)
            {
                sb.AppendLine($"- {service.ServiceName}: {service.Summary}");
                if (service.RecommendedUseCases.Count > 0)
                {
                    sb.AppendLine($"  UseCases: {string.Join(", ", service.RecommendedUseCases)}");
                }
            }

            sb.AppendLine();
        }

        if (evidence.Patterns.Count > 0)
        {
            sb.AppendLine("Pattern Hints:");
            foreach (var pattern in evidence.Patterns)
            {
                sb.AppendLine($"- {pattern.Name}: {pattern.Summary}");
                sb.AppendLine($"  SuggestedServices: {string.Join(", ", pattern.SuggestedServices)}");
            }

            sb.AppendLine();
        }

        if (evidence.PriorManifest is not null)
        {
            sb.AppendLine("Prior Manifest:");
            sb.AppendLine($"  Version: {evidence.PriorManifest.ManifestVersion}");
            sb.AppendLine($"  Summary: {evidence.PriorManifest.Summary}");
            sb.AppendLine();
        }

        sb.AppendLine("Task Objective:");
        sb.AppendLine(task.Objective);
        sb.AppendLine();

        sb.AppendLine("Allowed Tools:");
        foreach (var tool in task.AllowedTools)
        {
            sb.AppendLine($"- {tool}");
        }

        sb.AppendLine();

        sb.AppendLine("Allowed Sources:");
        foreach (var source in task.AllowedSources)
        {
            sb.AppendLine($"- {source}");
        }

        sb.AppendLine();
        sb.AppendLine("Important guidance:");
        sb.AppendLine("- Produce a simple, coherent MVP-quality Azure topology.");
        sb.AppendLine("- Prefer App Service over AKS unless AKS is truly necessary.");
        sb.AppendLine("- If Azure AI Search is required, include it explicitly.");
        sb.AppendLine("- If SQL metadata is implied, include a SQL datastore explicitly.");
        sb.AppendLine("- Use stable IDs such as svc-api, svc-search, ds-metadata where appropriate.");
        sb.AppendLine("- Return JSON only.");

        return sb.ToString();
    }
}
