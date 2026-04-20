using System.Globalization;
using System.Text;

using ArchLucid.Contracts.Architecture;
using ArchLucid.Contracts.DecisionTraces;
using ArchLucid.Contracts.Manifest;
using ArchLucid.Contracts.Metadata;

using Microsoft.Extensions.Logging;

namespace ArchLucid.Application.Pilots;

/// <summary>
/// Builds a sponsor-facing Markdown summary for a single architecture run (read-only projection).
/// </summary>
public sealed class FirstValueReportBuilder(
    IRunDetailQueryService runDetailQuery,
    ILogger<FirstValueReportBuilder> logger)
{
    /// <summary>
    /// Returns Markdown, or <see langword="null"/> when the run does not exist.
    /// When the run exists but is not committed, returns Markdown that states the gap explicitly.
    /// </summary>
    public async Task<string?> BuildMarkdownAsync(
        string runId,
        string apiBaseForLinks,
        CancellationToken cancellationToken = default)
    {
        if (runDetailQuery is null) throw new ArgumentNullException(nameof(runDetailQuery));
        if (logger is null) throw new ArgumentNullException(nameof(logger));
        if (string.IsNullOrWhiteSpace(runId)) throw new ArgumentException("Run id is required.", nameof(runId));

        string baseUrl = string.IsNullOrWhiteSpace(apiBaseForLinks)
            ? "http://localhost:5000"
            : apiBaseForLinks.Trim().TrimEnd('/');

        ArchitectureRunDetail? detail = await runDetailQuery.GetRunDetailAsync(runId, cancellationToken);

        if (detail is null)
        {
            logger.LogInformation("First-value report: run {RunId} not found.", runId);

            return null;
        }

        ArchitectureRun run = detail.Run;
        GoldenManifest? manifest = detail.Manifest;
        StringBuilder sb = new();

        sb.AppendLine("# ArchLucid — first value report (pilot)");
        sb.AppendLine();
        sb.AppendLine("This one-page summary is generated from committed run data in ArchLucid. Fill baseline cells during the pilot using your pre-ArchLucid measurements (see repository `docs/PILOT_ROI_MODEL.md`).");
        sb.AppendLine();

        AppendRunSection(sb, run, manifest, baseUrl);
        AppendFindingsSection(sb, detail);
        AppendElapsedSection(sb, run);
        AppendDecisionTraceSection(sb, detail, runId, baseUrl);
        AppendBaselinePlaceholderTable(sb);
        sb.AppendLine();
        sb.AppendLine("---");
        sb.AppendLine();
        sb.AppendLine("**Sponsor narrative (canonical):** repository `docs/EXECUTIVE_SPONSOR_BRIEF.md` (not served by this HTTP endpoint).");
        sb.AppendLine();
        sb.AppendLine($"*Generated from run `{run.RunId}`.*");

        return sb.ToString();
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

    private static void AppendFindingsSection(StringBuilder sb, ArchitectureRunDetail detail)
    {
        sb.AppendLine("## Findings by severity");
        sb.AppendLine();

        IOrderedEnumerable<KeyValuePair<string, int>> groups = detail.Results
            .Where(r => r is not null)
            .SelectMany(static r => r.Findings)
            .GroupBy(f => string.IsNullOrWhiteSpace(f.Severity) ? "Unknown" : f.Severity.Trim(), StringComparer.OrdinalIgnoreCase)
            .Select(g => new KeyValuePair<string, int>(g.Key, g.Count()))
            .OrderByDescending(static p => p.Value);

        List<KeyValuePair<string, int>> materialized = groups.ToList();

        if (materialized.Count == 0)
        {
            sb.AppendLine("_(No findings on agent results for this run.)_");
            sb.AppendLine();

            return;
        }

        sb.AppendLine("| Severity | Count |");
        sb.AppendLine("| --- | ---: |");

        foreach (KeyValuePair<string, int> row in materialized)
            sb.AppendLine($"| {row.Key} | {row.Value} |");


        sb.AppendLine();
    }

    private static void AppendElapsedSection(StringBuilder sb, ArchitectureRun run)
    {
        sb.AppendLine("## Time to committed output");
        sb.AppendLine();

        if (run.CompletedUtc is null)
        {
            sb.AppendLine("_(Run not in a terminal state — elapsed time not computed.)_");
            sb.AppendLine();

            return;
        }

        TimeSpan wall = run.CompletedUtc.Value - run.CreatedUtc;
        sb.AppendLine($"Wall-clock from run creation to terminal state: **{wall}** (UTC timestamps on the run record).");
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

    private static void AppendBaselinePlaceholderTable(StringBuilder sb)
    {
        sb.AppendLine("## Baseline vs pilot (fill during pilot)");
        sb.AppendLine();
        sb.AppendLine("| Pilot metric (see PILOT_ROI_MODEL.md) | Baseline (before) | During pilot | Notes |");
        sb.AppendLine("| --- | --- | --- | --- |");
        sb.AppendLine("| Time to committed manifest |  |  |  |");
        sb.AppendLine("| Time to reviewable artifact package |  |  |  |");
        sb.AppendLine("| Manual preparation effort |  |  |  |");
        sb.AppendLine("| Decision traceability |  |  |  |");
        sb.AppendLine();
    }
}
