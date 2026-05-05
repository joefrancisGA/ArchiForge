using System.Text;
using System.Text.Json;

using ArchLucid.AgentRuntime.Prompts;
using ArchLucid.Contracts.Abstractions.Agents;
using ArchLucid.Contracts.Agents;
using ArchLucid.Contracts.Common;
using ArchLucid.Contracts.Requests;
using ArchLucid.Core.Audit;
using ArchLucid.Core.Scoping;

using Microsoft.Extensions.Options;

namespace ArchLucid.AgentRuntime;

/// <summary>
///     <see cref="Contracts.Common.AgentType.Critic" /> handler: cross-checks the implied architecture for gaps and
///     contradictions.
/// </summary>
public sealed class CriticAgentHandler(
    IAgentCompletionClient completionClient,
    IAgentResultParser resultParser,
    IAgentExecutionTraceRecorder traceRecorder,
    IAgentSystemPromptCatalog systemPromptCatalog,
    IAuditService auditService,
    IScopeContextProvider scopeContextProvider,
    IOptionsMonitor<AgentSchemaRemediationOptions> schemaRemediationOptions)
    : IAgentHandler
{
    private static readonly JsonSerializerOptions TraceJsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    public AgentType AgentType => AgentType.Critic;

    /// <inheritdoc />
    public string AgentTypeKey => AgentTypeKeys.Critic;

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

        ResolvedSystemPrompt systemResolved = systemPromptCatalog.Resolve(AgentType.Critic);
        string systemPrompt = systemResolved.Text;
        AgentPromptActivityTags.Apply(systemResolved);
        AgentPromptReproMetadata promptRepro = systemResolved.ToReproMetadata();

        string baseUserPrompt = BuildUserPrompt(runId, request, evidence, task);

        string lastCompletionJson = string.Empty;

        try
        {
            (string rawJson, AgentResult parsed) = await LlmAgentSchemaCompletion.CompleteAsync(
                completionClient,
                resultParser,
                schemaRemediationOptions,
                AgentType.Critic,
                runId,
                task.TaskId,
                systemPrompt,
                baseUserPrompt,
                cancellationToken);

            lastCompletionJson = rawJson;

            string parsedJson = JsonSerializer.Serialize(parsed, TraceJsonOptions);

            AgentCompletionTokenUsage.TryConsume(out int? inTok, out int? outTok);
            AgentCompletionModelMetadata.TryConsume(out string? modelDeploy, out string? modelVer);

            await traceRecorder.RecordAsync(
                runId,
                task.TaskId,
                AgentType.Critic,
                systemPrompt,
                baseUserPrompt,
                rawJson,
                parsedJson,
                true,
                null,
                promptRepro,
                inTok,
                outTok,
                modelDeploy,
                modelVer,
                cancellationToken: cancellationToken);

            return parsed;
        }
        catch (Exception ex)
        {
            AgentCompletionTokenUsage.TryConsume(out int? inTok, out int? outTok);
            AgentCompletionModelMetadata.TryConsume(out string? modelDeploy, out string? modelVer);

            if (ex is AgentResultSchemaViolationException sv)

                AgentResultSchemaViolationAudit.ScheduleLog(
                    auditService,
                    scopeContextProvider,
                    sv,
                    runId,
                    task.TaskId,
                    modelDeploy,
                    modelVer);

            await traceRecorder.RecordAsync(
                runId,
                task.TaskId,
                AgentType.Critic,
                systemPrompt,
                baseUserPrompt,
                lastCompletionJson,
                null,
                false,
                ex.Message,
                promptRepro,
                inTok,
                outTok,
                modelDeploy,
                modelVer,
                failureReasonCode: AgentHandlerExecutionFailureReason.ResolveFailureReasonCode(ex),
                cancellationToken: cancellationToken);

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
        sb.AppendLine("- Be skeptical but constructive.");
        sb.AppendLine("- Identify omissions that could materially weaken a secure Azure architecture.");
        sb.AppendLine("- Favor findings and warnings over redesign.");
        sb.AppendLine("- If observability, identity, or secret management are clearly under-specified, call that out.");
        sb.AppendLine("- Return JSON only.");

        return sb.ToString();
    }
}
