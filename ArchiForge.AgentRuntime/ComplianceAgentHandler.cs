using System.Text;
using System.Text.Json;
using ArchiForge.Contracts.Agents;
using ArchiForge.Contracts.Common;
using ArchiForge.Contracts.Requests;

namespace ArchiForge.AgentRuntime;

public sealed class ComplianceAgentHandler(
    IAgentCompletionClient completionClient,
    IAgentResultParser resultParser,
    IAgentExecutionTraceRecorder traceRecorder)
    : IAgentHandler
{
    private static readonly JsonSerializerOptions TraceJsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    public AgentType AgentType => AgentType.Compliance;

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
            rawJson = await completionClient.CompleteJsonAsync(
                systemPrompt,
                userPrompt,
                cancellationToken);

            var parsed = resultParser.ParseAndValidate(
                rawJson,
                expectedRunId: runId,
                expectedTaskId: task.TaskId,
                expectedAgentType: AgentType.Compliance);

            var parsedJson = JsonSerializer.Serialize(parsed, TraceJsonOptions);

            await traceRecorder.RecordAsync(
                runId,
                task.TaskId,
                AgentType.Compliance,
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
            await traceRecorder.RecordAsync(
                runId,
                task.TaskId,
                AgentType.Compliance,
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
You are the ArchiForge Compliance Agent.

Your responsibility is to evaluate architecture requests for governance and control requirements.

You must return ONLY valid JSON that can be deserialized into an AgentResult object.

Do not include markdown.
Do not include commentary outside JSON.
Do not wrap the response in code fences.

Rules:
1. AgentType must be "Compliance".
2. RunId and TaskId must exactly match the values provided by the user prompt.
3. Confidence must be between 0.0 and 1.0.
4. ProposedChanges may include only:
   - RequiredControls
   - Warnings
5. You may include Findings related to compliance, policy, security baseline, or mandatory controls.
6. Do not add services, datastores, or relationships.
7. Do not produce cost estimates.
8. Prefer standard enterprise controls when clearly implied by constraints and required capabilities.
9. Keep the result conservative and governance-focused.

Use these enum string values exactly where needed:

AgentType:
- Compliance

Return JSON matching this conceptual shape:

{
  "resultId": "string",
  "taskId": "string",
  "runId": "string",
  "agentType": "Compliance",
  "claims": ["string"],
  "evidenceRefs": ["string"],
  "confidence": 0.0,
  "findings": [
    {
      "findingId": "string",
      "sourceAgent": "Compliance",
      "severity": "Info",
      "category": "Compliance",
      "message": "string",
      "evidenceRefs": ["string"]
    }
  ],
  "proposedChanges": {
    "proposalId": "string",
    "sourceAgent": "Compliance",
    "addedServices": [],
    "addedDatastores": [],
    "addedRelationships": [],
    "requiredControls": ["string"],
    "warnings": ["string"]
  },
  "createdUtc": "2026-03-15T14:00:00Z"
}

Important guidance:
- Use standard control names consistently, such as:
  - Managed Identity
  - Private Endpoints
  - Private Networking
  - Key Vault
  - Encryption At Rest
  - Diagnostic Logging
  - RBAC
- Findings should be short, machine-friendly, and reusable where possible.
- If a control is required, place it in ProposedChanges.RequiredControls.
""";
    }

    private static string BuildUserPrompt(
        string runId,
        ArchitectureRequest request,
        AgentEvidencePackage evidence,
        AgentTask task)
    {
        var sb = new StringBuilder();

        sb.AppendLine("Generate a compliance AgentResult.");
        sb.AppendLine();

        sb.AppendLine($"RunId: {runId}");
        sb.AppendLine($"TaskId: {task.TaskId}");
        sb.AppendLine($"AgentType: Compliance");
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
        sb.AppendLine("- Infer mandatory controls conservatively from constraints and required capabilities.");
        sb.AppendLine("- If managed identity is explicitly required, include Managed Identity.");
        sb.AppendLine("- If private endpoints or private networking are required, include Private Endpoints and/or Private Networking.");
        sb.AppendLine("- If encryption is required, include Encryption At Rest.");
        sb.AppendLine("- If secrets are likely present, include Key Vault.");
        sb.AppendLine("- Prefer reusable machine-friendly findings such as ManagedIdentityRequired or PrivateNetworkingRequired.");
        sb.AppendLine("- Return JSON only.");

        return sb.ToString();
    }
}
