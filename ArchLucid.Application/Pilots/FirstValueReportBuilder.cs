using System.Globalization;
using System.Text;

using ArchLucid.Application.Value;
using ArchLucid.Contracts.Architecture;
using ArchLucid.Contracts.DecisionTraces;
using ArchLucid.Contracts.Explanation;
using ArchLucid.Contracts.Manifest;
using ArchLucid.Contracts.Metadata;
using ArchLucid.Contracts.ValueReports;
using ArchLucid.Core.Scoping;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ArchLucid.Application.Pilots;

/// <summary>
/// Builds a sponsor-facing Markdown summary for a single architecture run (read-only projection).
/// </summary>
/// <remarks>
/// Computed deltas (wall-clock, findings-by-severity, audit rows, LLM calls, top-severity evidence chain) are
/// resolved by <see cref="IPilotRunDeltaComputer"/> so this builder and <see cref="SponsorOnePagerPdfBuilder"/>
/// stay in lockstep — the same numbers appear in the Markdown sibling and in the sponsor PDF wrapper.
/// The review-cycle delta section uses the same <see cref="ValueReportSnapshot"/> as the tenant value-report DOCX
/// (default 30-day UTC window ending now; see <c>ValueReportController</c>).
/// </remarks>
public sealed class FirstValueReportBuilder(
    IRunDetailQueryService runDetailQuery,
    IPilotRunDeltaComputer deltaComputer,
    ValueReportBuilder valueReportBuilder,
    IScopeContextProvider scopeProvider,
    IExecutionProvenanceFooterRenderer executionProvenanceFooter,
    IConfiguration configuration,
    ILogger<FirstValueReportBuilder> logger)
{
    /// <summary>Sponsor-facing banner appended above any computed line for runs that match the demo seed.</summary>
    private const string DemoTenantBanner = "_demo tenant — replace before publishing._";

    private readonly IRunDetailQueryService _runDetailQuery =
        runDetailQuery ?? throw new ArgumentNullException(nameof(runDetailQuery));

    private readonly IPilotRunDeltaComputer _deltaComputer =
        deltaComputer ?? throw new ArgumentNullException(nameof(deltaComputer));

    private readonly ValueReportBuilder _valueReportBuilder =
        valueReportBuilder ?? throw new ArgumentNullException(nameof(valueReportBuilder));

    private readonly IScopeContextProvider _scopeProvider =
        scopeProvider ?? throw new ArgumentNullException(nameof(scopeProvider));

    private readonly IExecutionProvenanceFooterRenderer _executionProvenanceFooter =
        executionProvenanceFooter ?? throw new ArgumentNullException(nameof(executionProvenanceFooter));

    private readonly IConfiguration _configuration =
        configuration ?? throw new ArgumentNullException(nameof(configuration));

    private readonly ILogger<FirstValueReportBuilder> _logger =
        logger ?? throw new ArgumentNullException(nameof(logger));

    /// <summary>
    /// Returns Markdown, or <see langword="null"/> when the run does not exist.
    /// When the run exists but is not committed, returns Markdown that states the gap explicitly.
    /// </summary>
    public async Task<string?> BuildMarkdownAsync(
        string runId,
        string apiBaseForLinks,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(runId)) throw new ArgumentException("Run id is required.", nameof(runId));

        string baseUrl = string.IsNullOrWhiteSpace(apiBaseForLinks)
            ? "http://localhost:5000"
            : apiBaseForLinks.Trim().TrimEnd('/');

        ArchitectureRunDetail? detail = await _runDetailQuery.GetRunDetailAsync(runId, cancellationToken);

        if (detail is null)
        {
            _logger.LogInformation("First-value report: run {RunId} not found.", runId);

            return null;
        }

        PilotRunDeltas deltas = await _deltaComputer.ComputeAsync(detail, cancellationToken);

        ScopeContext scope = _scopeProvider.GetCurrentScope();
        DateTimeOffset end = DateTimeOffset.UtcNow;
        DateTimeOffset start = end.AddDays(-30);
        ValueReportSnapshot valueWindowSnapshot = await _valueReportBuilder.BuildAsync(
            scope.TenantId,
            scope.WorkspaceId,
            scope.ProjectId,
            start,
            end,
            cancellationToken);

        ArchitectureRun run = detail.Run;
        GoldenManifest? manifest = detail.Manifest;
        StringBuilder sb = new();

        sb.AppendLine("# ArchLucid — first value report (pilot)");
        sb.AppendLine();
        sb.AppendLine("This one-page summary is generated from committed run data in ArchLucid. The **computed deltas** below replace the legacy baseline placeholders for the numbers ArchLucid can derive on its own; the qualitative baseline table at the bottom is still operator-filled. See repository `docs/PILOT_ROI_MODEL.md` §4 for the full metric catalog.");
        sb.AppendLine();

        if (run.RealModeFellBackToSimulator)
        {
            sb.AppendLine(_executionProvenanceFooter.BuildYellowSimulatorSubstitutionCallout());
            sb.AppendLine();
        }

        if (deltas.IsDemoTenant)
        {
            sb.AppendLine("> " + DemoTenantBanner + " The numbers below come from the seeded Contoso Retail Modernization dataset and MUST NOT be quoted as a real-customer outcome.");
            sb.AppendLine();
        }

        AppendRunSection(sb, run, manifest, baseUrl);
        AppendComputedDeltasSection(sb, deltas);
        ValueReportReviewCycleSectionFormatter.AppendMarkdownSection(sb, valueWindowSnapshot);
        AppendFindingFeedbackMarkdownSection(sb, valueWindowSnapshot);
        AppendFindingsSection(sb, deltas);
        AppendElapsedSection(sb, deltas);
        AppendDecisionTraceSection(sb, detail, runId, baseUrl);
        AppendEvidenceChainSection(sb, deltas);
        AppendBaselinePlaceholderTable(sb);
        sb.AppendLine();
        sb.AppendLine("---");
        sb.AppendLine();
        sb.AppendLine(_executionProvenanceFooter.BuildFooterMarkdown(BuildProvenanceInput(run, deltas)));
        sb.AppendLine();
        sb.AppendLine("---");
        sb.AppendLine();
        sb.AppendLine("**Sponsor narrative (canonical):** repository `docs/EXECUTIVE_SPONSOR_BRIEF.md` (not served by this HTTP endpoint).");
        sb.AppendLine();
        sb.AppendLine($"*Generated from run `{run.RunId}`.*");

        return sb.ToString();
    }

    private ExecutionProvenanceFooterInput BuildProvenanceInput(ArchitectureRun run, PilotRunDeltas deltas)
    {
        string hostMode = _configuration["AgentExecution:Mode"]?.Trim() ?? "Simulator";
        string? hostDeployment = _configuration["AzureOpenAI:DeploymentName"]?.Trim();

        return new ExecutionProvenanceFooterInput(
            run.RealModeFellBackToSimulator,
            run.PilotAoaiDeploymentSnapshot,
            hostMode,
            hostDeployment,
            deltas.LlmCallCount);
    }

    private static void AppendRunSection(StringBuilder sb, ArchitectureRun run, GoldenManifest? manifest, string baseUrl)
    {
        sb.AppendLine("## Run");
        sb.AppendLine();
        sb.AppendLine("| Field | Value |");
        sb.AppendLine("| --- | --- |");
        sb.AppendLine($"| Run id | `{run.RunId}` |");
        sb.AppendLine($"| Status | `{run.Status}` |");
        sb.AppendLine($"| Request id | `{run.RequestId}` |");
        sb.AppendLine($"| Created (UTC) | `{run.CreatedUtc:O}` |");
        sb.AppendLine($"| Completed (UTC) | `{(run.CompletedUtc is null ? "(pending)" : run.CompletedUtc.Value.ToString("O", CultureInfo.InvariantCulture))}` |");

        if (manifest is null)
        {
            sb.AppendLine("| Committed manifest | _(not available — run may not be committed yet)_ |");
            sb.AppendLine();

            return;
        }

        sb.AppendLine($"| System | `{manifest.SystemName}` |");
        sb.AppendLine($"| Manifest version | `{manifest.Metadata.ManifestVersion}` |");
        sb.AppendLine($"| Commit snapshot (UTC) | `{manifest.Metadata.CreatedUtc:O}` |");
        sb.AppendLine("| Environment (capture) | _(from original architecture request — add during pilot)_ |");
        sb.AppendLine();
        sb.AppendLine("### Evidence links");
        sb.AppendLine();
        sb.AppendLine($"- [Run detail JSON]({baseUrl}/v1/architecture/run/{run.RunId}) (`GET /v1/architecture/run/{{runId}}`)");
        sb.AppendLine($"- [Decision nodes]({baseUrl}/v1/architecture/run/{run.RunId}/decisions) (`GET /v1/architecture/run/{{runId}}/decisions`) — after commit");
        sb.AppendLine();
    }

    /// <summary>
    /// Computed-deltas table — the single block sponsors should look at first. Every row is derived from persisted
    /// run state via <see cref="IPilotRunDeltaComputer"/>; see field-by-field docs on <see cref="PilotRunDeltas"/>.
    /// </summary>
    private static void AppendFindingFeedbackMarkdownSection(StringBuilder sb, ValueReportSnapshot snapshot)
    {
        sb.AppendLine("## Finding feedback (thumbs, tenant window)");
        sb.AppendLine();
        sb.AppendLine("| Metric | Value |");
        sb.AppendLine("| --- | ---: |");
        sb.AppendLine(
            $"| Net score (up − down) | {snapshot.FindingFeedbackNetScore.ToString(CultureInfo.InvariantCulture)} |");
        sb.AppendLine(
            $"| Votes recorded | {snapshot.FindingFeedbackVoteCount.ToString(CultureInfo.InvariantCulture)} |");
        sb.AppendLine();
    }

    private static void AppendComputedDeltasSection(StringBuilder sb, PilotRunDeltas deltas)
    {
        sb.AppendLine("## Computed deltas (from this run)");
        sb.AppendLine();

        if (deltas.IsDemoTenant)
        {
            sb.AppendLine(DemoTenantBanner);
            sb.AppendLine();
        }

        sb.AppendLine("| Metric | Value | Source |");
        sb.AppendLine("| --- | --- | --- |");
        sb.AppendLine($"| Time to committed manifest | {FormatTimeToCommit(deltas)} | `RunRecord.CreatedUtc` → `GoldenManifest.CommittedUtc` |");
        sb.AppendLine($"| Findings (total) | {deltas.FindingsBySeverity.Sum(static p => p.Value)} | `ArchitectureRunDetail.Results[*].Findings` |");
        sb.AppendLine($"| LLM calls for this run | {deltas.LlmCallCount} | `archlucid_llm_calls_per_run` (per-run trace count) |");
        sb.AppendLine($"| Audit rows for this run | {FormatAuditRowCount(deltas)} | `IAuditRepository.GetFilteredAsync(RunId)` |");
        sb.AppendLine();
    }

    private static string FormatTimeToCommit(PilotRunDeltas deltas)
    {
        if (deltas.TimeToCommittedManifest is not { } wall)
            return "_(pending — no committed manifest yet)_";

        return $"**{wall:c}** (committed `{deltas.ManifestCommittedUtc:O}`)";
    }

    private static string FormatAuditRowCount(PilotRunDeltas deltas)
    {
        if (deltas.AuditRowCount == 0)
            return "0";

        return deltas.AuditRowCountTruncated
            ? $"{deltas.AuditRowCount}+ _(query cap reached — exact count is at least this many)_"
            : deltas.AuditRowCount.ToString(CultureInfo.InvariantCulture);
    }

    private static void AppendFindingsSection(StringBuilder sb, PilotRunDeltas deltas)
    {
        sb.AppendLine("## Findings by severity");
        sb.AppendLine();

        if (deltas.FindingsBySeverity.Count == 0)
        {
            sb.AppendLine("_(No findings on agent results for this run.)_");
            sb.AppendLine();

            return;
        }

        sb.AppendLine("| Severity | Count |");
        sb.AppendLine("| --- | ---: |");

        foreach (KeyValuePair<string, int> row in deltas.FindingsBySeverity)
            sb.AppendLine($"| {row.Key} | {row.Value} |");

        sb.AppendLine();
    }

    private static void AppendElapsedSection(StringBuilder sb, PilotRunDeltas deltas)
    {
        sb.AppendLine("## Time to committed output");
        sb.AppendLine();

        if (deltas.TimeToCommittedManifest is not { } wall)
        {
            sb.AppendLine("_(Run has no committed manifest — elapsed time not computed.)_");
            sb.AppendLine();

            return;
        }

        sb.AppendLine($"Wall-clock from `RunRecord.CreatedUtc` to `GoldenManifest.CommittedUtc`: **{wall:c}**.");
        sb.AppendLine($"Created: `{deltas.RunCreatedUtc:O}` · Committed: `{deltas.ManifestCommittedUtc:O}`.");
        sb.AppendLine();
    }

    private static void AppendDecisionTraceSection(StringBuilder sb, ArchitectureRunDetail detail, string runId, string baseUrl)
    {
        sb.AppendLine("## Decision trace summary (top 5)");
        sb.AppendLine();

        List<DecisionTrace> traces = detail.DecisionTraces.Where(static t => t is not null).Take(5).ToList();

        if (traces.Count == 0)
        {
            sb.AppendLine("_(No decision traces on this run — typical before commit or for coordinator-only paths.)_");
            sb.AppendLine();

            return;
        }

        int index = 1;

        foreach (DecisionTrace trace in traces)
        {
            if (trace is RuleAuditTrace rule)
            {
                RuleAuditTracePayload p = rule.RuleAudit;
                sb.AppendLine($"{index}. **Rule audit** — rule set `{p.RuleSetId}` v`{p.RuleSetVersion}`; applied rules: {p.AppliedRuleIds.Count}, accepted findings: {p.AcceptedFindingIds.Count}, rejected: {p.RejectedFindingIds.Count}.");
            }
            else if (trace is RunEventTrace runEvent)
            {
                RunEventTracePayload p = runEvent.RunEvent;
                sb.AppendLine($"{index}. **Run event** — `{p.EventType}`: {p.EventDescription}");
            }
            else
            {
                sb.AppendLine($"{index}. **Trace** — `{trace.Kind}`");
            }

            index++;
        }

        sb.AppendLine();
        sb.AppendLine($"Full trace payloads: [GET /v1/architecture/run/{runId}]({baseUrl}/v1/architecture/run/{runId}) (`decisionTraces` array when present).");
        sb.AppendLine();
    }

    /// <summary>
    /// Renders the top-severity finding's evidence-chain pointers (manifest version, snapshot ids, related graph
    /// nodes, agent execution traces) so a sponsor can hand a reviewer a single ID list to trace the decision.
    /// </summary>
    private static void AppendEvidenceChainSection(StringBuilder sb, PilotRunDeltas deltas)
    {
        sb.AppendLine("## Top-severity finding — evidence chain excerpt");
        sb.AppendLine();

        if (deltas.TopFindingId is null)
        {
            sb.AppendLine("_(No findings on this run; evidence-chain excerpt skipped.)_");
            sb.AppendLine();

            return;
        }

        sb.AppendLine($"Selected finding: `{deltas.TopFindingId}` (severity `{deltas.TopFindingSeverity ?? "Unknown"}`).");
        sb.AppendLine();

        FindingEvidenceChainResponse? chain = deltas.TopFindingEvidenceChain;

        if (chain is null)
        {
            sb.AppendLine("_(Evidence chain unavailable — the top-severity finding is not present in the persisted FindingsSnapshot, or the chain service could not resolve it. Review the full run detail JSON for an alternate selection.)_");
            sb.AppendLine();

            return;
        }

        sb.AppendLine("| Pointer | Value |");
        sb.AppendLine("| --- | --- |");
        sb.AppendLine($"| Manifest version | `{chain.ManifestVersion ?? "(none)"}` |");
        sb.AppendLine($"| Findings snapshot id | `{FormatGuid(chain.FindingsSnapshotId)}` |");
        sb.AppendLine($"| Context snapshot id | `{FormatGuid(chain.ContextSnapshotId)}` |");
        sb.AppendLine($"| Graph snapshot id | `{FormatGuid(chain.GraphSnapshotId)}` |");
        sb.AppendLine($"| Decision trace id | `{FormatGuid(chain.DecisionTraceId)}` |");
        sb.AppendLine($"| Golden manifest id | `{FormatGuid(chain.GoldenManifestId)}` |");
        sb.AppendLine($"| Related graph nodes | {chain.RelatedGraphNodeIds.Count} |");
        sb.AppendLine($"| Agent execution traces | {chain.AgentExecutionTraceIds.Count} |");
        sb.AppendLine();
    }

    private static string FormatGuid(Guid? id) => id is null ? "(none)" : id.Value.ToString("D");

    private static void AppendBaselinePlaceholderTable(StringBuilder sb)
    {
        sb.AppendLine("## Qualitative baseline (operator-filled)");
        sb.AppendLine();
        sb.AppendLine("Use this table for the qualitative metrics ArchLucid cannot derive on its own. The numeric metrics (time-to-commit, findings counts, audit rows, LLM calls) are now in the **Computed deltas** section above.");
        sb.AppendLine();
        sb.AppendLine("| Pilot metric (see PILOT_ROI_MODEL.md) | Baseline (before) | During pilot | Notes |");
        sb.AppendLine("| --- | --- | --- | --- |");
        sb.AppendLine("| Time to reviewable artifact package |  |  |  |");
        sb.AppendLine("| Manual preparation effort |  |  |  |");
        sb.AppendLine("| Decision traceability (qualitative) |  |  |  |");
        sb.AppendLine("| Reviewer / sponsor confidence |  |  |  |");
        sb.AppendLine();
    }
}
