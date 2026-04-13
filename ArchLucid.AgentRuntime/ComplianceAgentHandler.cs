using System.Text;
using System.Text.Json;

using ArchLucid.AgentRuntime.Prompts;
using ArchLucid.Contracts.Agents;
using ArchLucid.Contracts.Common;
using ArchLucid.Contracts.Requests;
using ArchLucid.Core.Audit;
using ArchLucid.Core.Scoping;

namespace ArchLucid.AgentRuntime;

/// <summary>
/// <see cref="AgentType.Compliance"/> handler: evaluates policies and controls from the evidence package via the completion client.
/// </summary>
public sealed class ComplianceAgentHandler(
    IAgentCompletionClient completionClient,
    IAgentResultParser resultParser,
    IAgentExecutionTraceRecorder traceRecorder,
    IAgentSystemPromptCatalog systemPromptCatalog,
    IAuditService auditService,
    IScopeContextProvider scopeContextProvider)
    : IAgentHandler
{
    private static readonly JsonSerializerOptions TraceJsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    public AgentType AgentType => AgentType.Compliance;

    /// <inheritdoc />
    public string AgentTypeKey => AgentTypeKeys.Compliance;

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

        ResolvedSystemPrompt systemResolved = systemPromptCatalog.Resolve(AgentType.Compliance);
        string systemPrompt = systemResolved.Text;
        AgentPromptActivityTags.Apply(systemResolved);
        AgentPromptReproMetadata promptRepro = systemResolved.ToReproMetadata();
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
                expectedAgentType: AgentType.Compliance);

            string parsedJson = JsonSerializer.Serialize(parsed, TraceJsonOptions);

            AgentCompletionTokenUsage.TryConsume(out int? inTok, out int? outTok);
            AgentCompletionModelMetadata.TryConsume(out string? modelDeploy, out string? modelVer);

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
                promptRepro,
                inTok,
                outTok,
                modelDeploymentName: modelDeploy,
                modelVersion: modelVer,
                cancellationToken);

            return parsed;
        }
        catch (Exception ex)
        {
            AgentCompletionTokenUsage.TryConsume(out int? inTok, out int? outTok);
            AgentCompletionModelMetadata.TryConsume(out string? modelDeploy, out string? modelVer);

            if (ex is AgentResultSchemaViolationException sv)
            {
                AgentResultSchemaViolationAudit.ScheduleLog(
                    auditService,
                    scopeContextProvider,
                    sv,
                    runId,
                    task.TaskId,
                    modelDeploy,
                    modelVer);
            }

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
                promptRepro,
                inTok,
                outTok,
                modelDeploymentName: modelDeploy,
                modelVersion: modelVer,
                cancellationToken);

            throw;
        }
    }

    private static string BuildUserPrompt(
        string runId,
        ArchitectureRequest request,
        AgentEvidencePackage evidence,
        AgentTask task)
    {
        StringBuilder sb = new();

        sb.AppendLine("Generate a compliance AgentResult.");
        sb.AppendLine();

        sb.AppendLine($"RunId: {runId}");
        sb.AppendLine($"TaskId: {task.TaskId}");
        sb.AppendLine("AgentType: Compliance");
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
            
                sb.AppendLine($"- {constraint}");
            

            sb.AppendLine();
        }

        if (request.RequiredCapabilities.Count > 0)
        {
            sb.AppendLine("Required Capabilities:");
            foreach (string capability in request.RequiredCapabilities)
            
                sb.AppendLine($"- {capability}");
            

            sb.AppendLine();
        }

        if (request.Assumptions.Count > 0)
        {
            sb.AppendLine("Assumptions:");
            foreach (string assumption in request.Assumptions)
            
                sb.AppendLine($"- {assumption}");
            

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
                
                    sb.AppendLine($"  RequiredControls: {string.Join(", ", policy.RequiredControls)}");
                
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
                
                    sb.AppendLine($"  UseCases: {string.Join(", ", service.RecommendedUseCases)}");
                
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
        
            sb.AppendLine($"- {tool}");
        

        sb.AppendLine();

        sb.AppendLine("Allowed Sources:");
        foreach (string source in task.AllowedSources)
        
            sb.AppendLine($"- {source}");
        

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
