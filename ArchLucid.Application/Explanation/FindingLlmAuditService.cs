using ArchLucid.Contracts.Agents;
using ArchLucid.Contracts.Common;
using ArchLucid.Contracts.Explanation;
using ArchLucid.Core.Llm.Redaction;
using ArchLucid.Core.Scoping;
using ArchLucid.Decisioning.Models;
using ArchLucid.Persistence.Data.Repositories;
using ArchLucid.Persistence.Queries;

namespace ArchLucid.Application.Explanation;
/// <inheritdoc cref = "IFindingLlmAuditService"/>
public sealed class FindingLlmAuditService(IAuthorityQueryService authorityQuery, IScopeContextProvider scopeContextProvider, IAgentExecutionTraceRepository agentExecutionTraceRepository, IPromptRedactor promptRedactor) : IFindingLlmAuditService
{
    private readonly byte __primaryConstructorArgumentValidation = __ValidatePrimaryConstructorArguments(authorityQuery, scopeContextProvider, agentExecutionTraceRepository, promptRedactor);
    private static byte __ValidatePrimaryConstructorArguments(ArchLucid.Persistence.Queries.IAuthorityQueryService authorityQuery, ArchLucid.Core.Scoping.IScopeContextProvider scopeContextProvider, ArchLucid.Persistence.Data.Repositories.IAgentExecutionTraceRepository agentExecutionTraceRepository, ArchLucid.Core.Llm.Redaction.IPromptRedactor promptRedactor)
    {
        ArgumentNullException.ThrowIfNull(authorityQuery);
        ArgumentNullException.ThrowIfNull(scopeContextProvider);
        ArgumentNullException.ThrowIfNull(agentExecutionTraceRepository);
        ArgumentNullException.ThrowIfNull(promptRedactor);
        return (byte)0;
    }

    private readonly IAgentExecutionTraceRepository _agentExecutionTraceRepository = agentExecutionTraceRepository ?? throw new ArgumentNullException(nameof(agentExecutionTraceRepository));
    private readonly IAuthorityQueryService _authorityQuery = authorityQuery ?? throw new ArgumentNullException(nameof(authorityQuery));
    private readonly IPromptRedactor _promptRedactor = promptRedactor ?? throw new ArgumentNullException(nameof(promptRedactor));
    private readonly IScopeContextProvider _scopeContextProvider = scopeContextProvider ?? throw new ArgumentNullException(nameof(scopeContextProvider));
    /// <inheritdoc/>
    public async System.Threading.Tasks.Task<ArchLucid.Contracts.Explanation.FindingLlmAuditResult?> BuildAsync(Guid runId, string findingId, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(findingId);
        if (string.IsNullOrWhiteSpace(findingId))
            throw new ArgumentException("Finding id is required.", nameof(findingId));
        ScopeContext scope = _scopeContextProvider.GetCurrentScope();
        RunDetailDto? detail = await _authorityQuery.GetRunDetailAsync(scope, runId, cancellationToken);
        if (detail?.FindingsSnapshot?.Findings is not { Count: > 0 } findings)
            return null;
        Finding? match = findings.FirstOrDefault(f => string.Equals(f.FindingId, findingId, StringComparison.OrdinalIgnoreCase));
        if (match is null)
            return null;
        string runIdStr = runId.ToString("N");
        AgentExecutionTrace? trace = await ResolveTraceAsync(runIdStr, match, cancellationToken);
        if (trace is null)
            return null;
        PromptRedactionOutcome sys = _promptRedactor.Redact(ResolveSystemPrompt(trace));
        PromptRedactionOutcome usr = _promptRedactor.Redact(ResolveUserPrompt(trace));
        PromptRedactionOutcome raw = _promptRedactor.Redact(ResolveRawResponse(trace));
        Dictionary<string, int> merged = MergeCounts(sys.CountsByCategory, usr.CountsByCategory, raw.CountsByCategory);
        return new FindingLlmAuditResult
        {
            TraceId = trace.TraceId,
            AgentType = trace.AgentType.ToString(),
            SystemPromptRedacted = sys.Text,
            UserPromptRedacted = usr.Text,
            RawResponseRedacted = raw.Text,
            ModelDeploymentName = trace.ModelDeploymentName,
            ModelVersion = trace.ModelVersion,
            RedactionCountsByCategory = merged
        };
    }

    private async Task<AgentExecutionTrace?> ResolveTraceAsync(string runId, Finding finding, CancellationToken cancellationToken)
    {
        string? preferredId = finding.Trace.SourceAgentExecutionTraceId;
        if (!string.IsNullOrWhiteSpace(preferredId))
        {
            AgentExecutionTrace? direct = await _agentExecutionTraceRepository.GetByTraceIdAsync(preferredId.Trim(), cancellationToken);
            if (direct is not null)
                return direct;
        }

        IReadOnlyList<AgentExecutionTrace> traces = await _agentExecutionTraceRepository.GetByRunIdAsync(runId, cancellationToken);
        if (traces.Count == 0)
            return null;
        if (!Enum.TryParse(finding.EngineType, true, out AgentType engineAgent))
            return traces[0];
        AgentExecutionTrace? typed = traces.FirstOrDefault(t => t.AgentType == engineAgent);
        return typed ?? traces[0];
    }

    private static string ResolveSystemPrompt(AgentExecutionTrace trace)
    {
        return string.IsNullOrEmpty(trace.FullSystemPromptInline) ? trace.SystemPrompt : trace.FullSystemPromptInline;
    }

    private static string ResolveUserPrompt(AgentExecutionTrace trace)
    {
        return string.IsNullOrEmpty(trace.FullUserPromptInline) ? trace.UserPrompt : trace.FullUserPromptInline;
    }

    private static string ResolveRawResponse(AgentExecutionTrace trace)
    {
        return string.IsNullOrEmpty(trace.FullResponseInline) ? trace.RawResponse : trace.FullResponseInline;
    }

    private static Dictionary<string, int> MergeCounts(IReadOnlyDictionary<string, int> a, IReadOnlyDictionary<string, int> b, IReadOnlyDictionary<string, int> c)
    {
        Dictionary<string, int> merged = new(StringComparer.OrdinalIgnoreCase);
        Add(a);
        Add(b);
        Add(c);
        return merged;
        void Add(IReadOnlyDictionary<string, int> src)
        {
            foreach (KeyValuePair<string, int> kv in src)
                merged[kv.Key] = merged.GetValueOrDefault(kv.Key, 0) + kv.Value;
        }
    }
}