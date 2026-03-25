using System.Text;
using System.Text.Json;

using ArchiForge.Contracts.Agents;
using ArchiForge.Contracts.Common;
using ArchiForge.Contracts.Requests;

namespace ArchiForge.AgentRuntime;

public sealed class CriticAgentHandler(
    IAgentCompletionClient completionClient,
    IAgentResultParser resultParser,
    IAgentExecutionTraceRecorder traceRecorder)
    : IAgentHandler
{
    private static readonly JsonSerializerOptions TraceJsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    public AgentType AgentType => AgentType.Critic;

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

        string systemPrompt = BuildSystemPrompt();
        string userPrompt = BuildUserPrompt(runId, request, evidence, task);

        string rawJson = string.Empty;

        try
        {
            rawJson = await completionClient.CompleteJsonAsync(
                systemPrompt,
                userPrompt,
                cancellationToken);

            AgentResult parsed = resultParser.ParseAndValidate(
                rawJson,
                expectedRunId: runId,
                expectedTaskId: task.TaskId,
                expectedAgentType: AgentType.Critic);

            string parsedJson = JsonSerializer.Serialize(parsed, TraceJsonOptions);

            await traceRecorder.RecordAsync(
                runId,
                task.TaskId,
                AgentType.Critic,
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
                AgentType.Critic,
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
You are the ArchiForge Critic Agent.

Your job is to critique the proposed architecture direction implied by the request and identify missing elements, weak assumptions, or architectural risks.

You must return ONLY valid JSON that can be deserialized into an AgentResult object.

Do not include markdown.
Do not include commentary outside JSON.
Do not wrap the response in code fences.

Rules:
1. AgentType must be "Critic".
2. RunId and TaskId must exactly match the values provided by the user prompt.
3. Confidence must be between 0.0 and 1.0.
4. Your output is a critique and review, not a redesign.
5. You may emit:
   - Claims
   - Findings
   - Warnings
   - RequiredControls only if clearly required and obviously missing from a secure baseline
6. Do not add services, datastores, or relationships unless absolutely necessary to describe a critical missing architectural dependency.
7. Do not produce cost estimates.
8. Prefer conservative, review-oriented findings.
9. Use short, machine-friendly finding messages where practical.

Use these enum string values exactly where needed:

AgentType:
- Critic

Return JSON matching this conceptual shape:

{
  "resultId": "string",
  "taskId": "string",
  "runId": "string",
  "agentType": "Critic",
  "claims": ["string"],
  "evidenceRefs": ["string"],
  "confidence": 0.0,
  "findings": [
    {
      "findingId": "string",
      "sourceAgent": "Critic",
      "severity": "Info",
      "category": "Critic",
      "message": "string",
      "evidenceRefs": ["string"]
    }
  ],
  "proposedChanges": {
    "proposalId": "string",
    "sourceAgent": "Critic",
    "addedServices": [],
    "addedDatastores": [],
    "addedRelationships": [],
    "requiredControls": [],
    "warnings": ["string"]
  },
  "createdUtc": "2026-03-15T14:00:00Z"
}

Important review themes:
- missing identity boundaries
- missing secret management
- missing private networking assumptions
- missing observability / logging
- hidden operational complexity
- contradictions between simplicity and enterprise readiness
- risks created by under-specified architecture
""";
    }

    private static string BuildUserPrompt(
        string runId,
        ArchitectureRequest request,
        AgentEvidencePackage evidence,
        AgentTask task)
    {
        StringBuilder sb = new();

        sb.AppendLine("Generate a critic AgentResult.");
        sb.AppendLine();

        sb.AppendLine($"RunId: {runId}");
        sb.AppendLine($"TaskId: {task.TaskId}");
        sb.AppendLine("AgentType: Critic");
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
            foreach (string constraint in request.Constraints)
            {
                sb.AppendLine($"- {constraint}");
            }

            sb.AppendLine();
        }

        if (request.RequiredCapabilities.Count > 0)
        {
            sb.AppendLine("Required Capabilities:");
            foreach (string capability in request.RequiredCapabilities)
            {
                sb.AppendLine($"- {capability}");
            }

            sb.AppendLine();
        }

        if (request.Assumptions.Count > 0)
        {
            sb.AppendLine("Assumptions:");
            foreach (string assumption in request.Assumptions)
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
            foreach (PolicyEvidence policy in evidence.Policies)
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
            foreach (ServiceCatalogEvidence service in evidence.ServiceCatalog)
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
            foreach (PatternEvidence pattern in evidence.Patterns)
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
        foreach (string tool in task.AllowedTools)
        {
            sb.AppendLine($"- {tool}");
        }

        sb.AppendLine();

        sb.AppendLine("Allowed Sources:");
        foreach (string source in task.AllowedSources)
        {
            sb.AppendLine($"- {source}");
        }

        sb.AppendLine();
        sb.AppendLine("Important guidance:");
        sb.AppendLine("- Be skeptical but constructive.");
        sb.AppendLine("- Identify omissions that could materially weaken a secure Azure architecture.");
        sb.AppendLine("- Favor findings and warnings over redesign.");
        sb.AppendLine("- If observability, identity, or secret management are clearly under-specified, call that out.");
        sb.AppendLine("- Return JSON only.");

        return sb.ToString();
    }
}
