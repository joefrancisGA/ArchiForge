using ArchLucid.Contracts.Architecture;
using ArchLucid.Contracts.DecisionTraces;
using ArchLucid.Contracts.Findings;
using ArchLucid.Core.Explanation;
using ArchLucid.Core.Scoping;
using ArchLucid.Decisioning.Findings;
using ArchLucid.Decisioning.Models;
using ArchLucid.Persistence.Queries;

namespace ArchLucid.Application.Explanation;

/// <inheritdoc />
public sealed class RunRationaleService(
    IAuthorityQueryService authorityQuery,
    IRunDetailQueryService runDetailQuery) : IRunRationaleService
{
    private const string PipelineAuthority = "authority";

    private const string PipelineCoordinator = "coordinator";

    private const string KindRuleAudit = "ruleAudit";

    private const string KindRunEvent = "runEvent";

    /// <inheritdoc />
    public async Task<RunRationale?> GetRunRationaleAsync(ScopeContext scope, Guid runId, CancellationToken ct)
    {
        RunDetailDto? detail = await authorityQuery.GetRunDetailAsync(scope, runId, ct);

        if (detail is null)
            return null;

        bool explanationAvailable = detail.GoldenManifest is not null;
        bool provenanceAvailable =
            detail.GoldenManifest is not null
            && detail.GraphSnapshot is not null
            && detail.FindingsSnapshot is not null
            && detail.AuthorityTrace is not null;

        if (detail.FindingsSnapshot is not null)
            return BuildAuthorityRationale(detail, provenanceAvailable, explanationAvailable);

        ArchitectureRunDetail? coordinator = await runDetailQuery.GetRunDetailAsync(runId.ToString("N"), ct);

        return coordinator is not null
            ? BuildCoordinatorRationale(detail, coordinator, runId, provenanceAvailable, explanationAvailable)
            : BuildAuthorityRationaleWithoutFindings(detail, runId, provenanceAvailable, explanationAvailable);
    }

    private static RunRationale BuildAuthorityRationale(
        RunDetailDto detail,
        bool provenanceAvailable,
        bool explanationAvailable)
    {
        FindingsSnapshot snapshot = detail.FindingsSnapshot!;
        List<Finding> findings = snapshot.Findings;

        List<FindingRationale> mapped = findings
            .Select(f =>
            {
                TraceCompletenessScore score = ExplainabilityTraceCompletenessAnalyzer.AnalyzeFinding(f);

                return new FindingRationale
                {
                    FindingId = f.FindingId,
                    Title = f.Title,
                    Severity = f.Severity.ToString(),
                    Rationale = f.Rationale,
                    Category = f.Category,
                    EngineType = f.EngineType,
                    RelatedNodeIds = f.RelatedNodeIds,
                    RecommendedActions = f.RecommendedActions,
                    TraceCompleteness = ToApiScore(score)
                };
            })
            .ToList();

        List<DecisionTraceEntry> traces = MapAuthorityDecisionTraces(detail);

        return new RunRationale
        {
            RunId = detail.Run.RunId,
            PipelineType = PipelineAuthority,
            Summary = BuildAuthoritySummary(detail, findings.Count),
            Findings = mapped,
            DecisionTraceEntries = traces,
            ProvenanceAvailable = provenanceAvailable,
            ExplanationAvailable = explanationAvailable
        };
    }

    private static RunRationale BuildAuthorityRationaleWithoutFindings(
        RunDetailDto detail,
        Guid runId,
        bool provenanceAvailable,
        bool explanationAvailable)
    {
        List<DecisionTraceEntry> traces = MapAuthorityDecisionTraces(detail);

        return new RunRationale
        {
            RunId = runId,
            PipelineType = PipelineAuthority,
            Summary = BuildAuthoritySummary(detail, 0),
            Findings = [],
            DecisionTraceEntries = traces,
            ProvenanceAvailable = provenanceAvailable,
            ExplanationAvailable = explanationAvailable
        };
    }

    private static RunRationale BuildCoordinatorRationale(
        RunDetailDto authorityDetail,
        ArchitectureRunDetail coordinator,
        Guid runId,
        bool provenanceAvailable,
        bool explanationAvailable)
    {
        List<FindingRationale> findings = coordinator.Results
            .SelectMany(r => r.Findings)
            .Select(MapArchitectureFinding)
            .ToList();

        List<DecisionTraceEntry> traces = coordinator.DecisionTraces
            .Select(MapCoordinatorTrace)
            .Where(static e => e is not null)
            .Cast<DecisionTraceEntry>()
            .ToList();

        return new RunRationale
        {
            RunId = runId,
            PipelineType = PipelineCoordinator,
            Summary = BuildCoordinatorSummary(authorityDetail, coordinator, findings.Count),
            Findings = findings,
            DecisionTraceEntries = traces,
            ProvenanceAvailable = provenanceAvailable,
            ExplanationAvailable = explanationAvailable
        };
    }

    private static List<DecisionTraceEntry> MapAuthorityDecisionTraces(RunDetailDto detail)
    {
        if (detail.AuthorityTrace is not RuleAuditTrace ruleAudit)
            return [];

        return [MapRuleAudit(ruleAudit.RuleAudit)];
    }

    private static DecisionTraceEntry? MapCoordinatorTrace(DecisionTrace trace)
    {
        switch (trace)
        {
            case RuleAuditTrace ra:
                return MapRuleAudit(ra.RuleAudit);
            case RunEventTrace re:
                return MapRunEvent(re.RunEvent);
            default:
                return null;
        }
    }

    private static DecisionTraceEntry MapRuleAudit(RuleAuditTracePayload p)
    {
        int applied = p.AppliedRuleIds.Count;
        int accepted = p.AcceptedFindingIds.Count;
        int rejected = p.RejectedFindingIds.Count;

        return new DecisionTraceEntry
        {
            TraceId = p.DecisionTraceId.ToString("N"),
            CreatedUtc = ToUtcOffset(p.CreatedUtc),
            Kind = KindRuleAudit,
            Description =
                $"Rule set {p.RuleSetId} ({p.RuleSetVersion}): {applied} rule(s) applied, {accepted} finding(s) accepted, {rejected} rejected.",
            Details = new Dictionary<string, object>
            {
                ["tenantId"] = p.TenantId.ToString("N"),
                ["workspaceId"] = p.WorkspaceId.ToString("N"),
                ["projectId"] = p.ProjectId.ToString("N"),
                ["decisionTraceId"] = p.DecisionTraceId.ToString("N"),
                ["runId"] = p.RunId.ToString("N"),
                ["ruleSetId"] = p.RuleSetId,
                ["ruleSetVersion"] = p.RuleSetVersion,
                ["ruleSetHash"] = p.RuleSetHash,
                ["appliedRuleIds"] = p.AppliedRuleIds,
                ["acceptedFindingIds"] = p.AcceptedFindingIds,
                ["rejectedFindingIds"] = p.RejectedFindingIds,
                ["notes"] = p.Notes
            }
        };
    }

    private static DecisionTraceEntry MapRunEvent(RunEventTracePayload p)
    {
        Dictionary<string, object> details = new(StringComparer.Ordinal);

        foreach (KeyValuePair<string, string> kv in p.Metadata)
            details[kv.Key] = kv.Value;

        details["runId"] = p.RunId;
        details["eventType"] = p.EventType;

        return new DecisionTraceEntry
        {
            TraceId = p.TraceId,
            CreatedUtc = ToUtcOffset(p.CreatedUtc),
            Kind = KindRunEvent,
            Description = string.IsNullOrWhiteSpace(p.EventDescription) ? p.EventType : p.EventDescription,
            Details = details
        };
    }

    private static FindingRationale MapArchitectureFinding(ArchitectureFinding f)
    {
        string message = f.Message;

        return new FindingRationale
        {
            FindingId = f.FindingId,
            Title = message,
            Severity = f.Severity.ToString(),
            Rationale = message,
            Category = f.Category,
            EngineType = f.SourceAgent.ToString(),
            RelatedNodeIds = f.EvidenceRefs,
            RecommendedActions = [],
            TraceCompleteness = null
        };
    }

    private static FindingTraceCompletenessScore ToApiScore(TraceCompletenessScore s)
    {
        return new FindingTraceCompletenessScore
        {
            FindingId = s.FindingId,
            EngineType = s.EngineType,
            HasGraphNodeIds = s.HasGraphNodeIds,
            HasRulesApplied = s.HasRulesApplied,
            HasDecisionsTaken = s.HasDecisionsTaken,
            HasAlternativePaths = s.HasAlternativePaths,
            HasNotes = s.HasNotes,
            PopulatedFieldCount = s.PopulatedFieldCount,
            CompletenessRatio = s.CompletenessRatio,
            MissingTraceFields = [..s.MissingTraceFields]
        };
    }

    private static string BuildAuthoritySummary(RunDetailDto detail, int findingCount)
    {
        string? fromManifest = detail.GoldenManifest?.Metadata.Summary;

        if (!string.IsNullOrWhiteSpace(fromManifest))
            return fromManifest.Trim();

        string? desc = detail.Run.Description;

        return !string.IsNullOrWhiteSpace(desc) ? desc.Trim() : $"Authority run with {findingCount} finding(s).";
    }

    private static string BuildCoordinatorSummary(
        RunDetailDto authorityDetail,
        ArchitectureRunDetail coordinator,
        int findingCount)
    {
        string? authorityManifestSummary = authorityDetail.GoldenManifest?.Metadata.Summary;

        if (!string.IsNullOrWhiteSpace(authorityManifestSummary))
            return authorityManifestSummary.Trim();

        if (coordinator.Manifest is { } manifest)
        {
            return !string.IsNullOrWhiteSpace(manifest.Metadata.ChangeDescription)
                ? manifest.Metadata.ChangeDescription.Trim()
                : $"{manifest.SystemName}: coordinator run ({coordinator.Run.Status}), {findingCount} agent finding(s).";
        }

        return $"Coordinator run ({coordinator.Run.Status}) with {findingCount} agent finding(s).";
    }

    private static DateTimeOffset ToUtcOffset(DateTime utc)
    {
        return new DateTimeOffset(DateTime.SpecifyKind(utc, DateTimeKind.Utc), TimeSpan.Zero);
    }
}
