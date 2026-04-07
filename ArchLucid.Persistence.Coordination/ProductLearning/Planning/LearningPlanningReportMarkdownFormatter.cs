using System.Globalization;
using System.Text;

using ArchLucid.Contracts.ProductLearning.Planning;

namespace ArchLucid.Persistence.Coordination.ProductLearning.Planning;

/// <summary>Deterministic markdown for <see cref="LearningPlanningReportDocument"/> (fixed section order, invariant numeric formatting).</summary>
public static class LearningPlanningReportMarkdownFormatter
{
    public static string Format(LearningPlanningReportDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(document.Summary);

        StringBuilder sb = new();
        IFormatProvider inv = CultureInfo.InvariantCulture;

        sb.AppendLine("# ArchiForge planning report (59R)");
        sb.AppendLine();
        sb.AppendLine($"Generated (UTC): {document.GeneratedUtc:O}");
        sb.AppendLine();

        sb.AppendLine("## Summary");
        sb.AppendLine($"- Themes: {document.Summary.ThemeCount.ToString(inv)}");
        sb.AppendLine($"- Plans: {document.Summary.PlanCount.ToString(inv)}");
        sb.AppendLine($"- Theme evidence (signals): {document.Summary.TotalThemeEvidenceSignals.ToString(inv)}");
        sb.AppendLine($"- Linked pilot signals (across plans): {document.Summary.TotalLinkedSignalsAcrossPlans.ToString(inv)}");
        sb.AppendLine(
            $"- Max plan priority score: {(document.Summary.MaxPlanPriorityScore?.ToString(inv) ?? "—")}");
        sb.AppendLine();

        sb.AppendLine("## Top improvement themes");
        sb.AppendLine();

        if (document.Themes.Count == 0)
        {
            sb.AppendLine("_No themes in scope._");
            sb.AppendLine();
        }
        else
        {
            for (int i = 0; i < document.Themes.Count; i++)
            {
                LearningPlanningReportThemeEntry t = document.Themes[i];
                sb.AppendLine($"### {i + 1}. {EscapeHeading(t.Title)}");
                sb.AppendLine($"- Theme id: `{t.ThemeId:D}`");
                sb.AppendLine($"- Key: `{EscapeInline(t.ThemeKey)}`");
                sb.AppendLine($"- Severity: {EscapeInline(t.SeverityBand)}");
                sb.AppendLine($"- Status: {EscapeInline(t.Status)}");
                sb.AppendLine($"- Evidence signals: {t.EvidenceSignalCount.ToString(inv)}");
                sb.AppendLine($"- Distinct runs: {t.DistinctRunCount.ToString(inv)}");
                sb.AppendLine($"- Summary: {OneLine(t.Summary)}");
                sb.AppendLine();
            }
        }

        sb.AppendLine("## Prioritized improvement plans");
        sb.AppendLine();

        if (document.Plans.Count == 0)
        {
            sb.AppendLine("_No plans in scope._");
        }
        else
        {
            for (int i = 0; i < document.Plans.Count; i++)
            {
                LearningPlanningReportPlanEntry p = document.Plans[i];
                sb.AppendLine($"### {i + 1}. {EscapeHeading(p.Title)}");
                sb.AppendLine($"- Plan id: `{p.PlanId:D}`");
                sb.AppendLine($"- Priority score: {p.PriorityScore.ToString(inv)}");

                if (!string.IsNullOrWhiteSpace(p.PriorityExplanation))
                {
                    sb.AppendLine($"- Priority note: {OneLine(p.PriorityExplanation)}");
                }

                sb.AppendLine($"- Status: {EscapeInline(p.Status)}");
                sb.AppendLine($"- Created (UTC): {p.CreatedUtc:O}");
                sb.AppendLine($"- Theme: {EscapeInline(p.ThemeTitle)} (`{p.ThemeId:D}`)");
                sb.AppendLine($"- Action steps: {p.ActionStepCount.ToString(inv)}");
                sb.AppendLine($"- Summary: {OneLine(p.Summary)}");
                sb.AppendLine();

                LearningPlanningReportPlanEvidenceBlock ev = p.Evidence;
                sb.AppendLine("#### Evidence");
                sb.AppendLine(
                    $"- Totals — signals: {ev.LinkedSignalCount.ToString(inv)}, artifacts: {ev.LinkedArtifactCount.ToString(inv)}, architecture runs: {ev.LinkedArchitectureRunCount.ToString(inv)}");
                sb.AppendLine();

                sb.AppendLine("##### Pilot signal ids");
                AppendBulletList(sb, ev.Signals.Select(FormatSignalRef));
                sb.AppendLine();

                sb.AppendLine("##### Artifact links");
                AppendBulletList(sb, ev.Artifacts.Select(FormatArtifactRef));
                sb.AppendLine();

                sb.AppendLine("##### Architecture run ids");
                AppendBulletList(sb, ev.ArchitectureRunIds.Select(r => $"`{EscapeInline(r)}`"));
                sb.AppendLine();
            }
        }

        return sb.ToString().TrimEnd() + Environment.NewLine;
    }

    private static string FormatSignalRef(LearningPlanningReportSignalRef s)
    {
        string triage = string.IsNullOrWhiteSpace(s.TriageStatusSnapshot) ? "—" : EscapeInline(s.TriageStatusSnapshot!);

        return $"`{s.SignalId:D}` (triage snapshot: {triage})";
    }

    private static string FormatArtifactRef(LearningPlanningReportArtifactRef a)
    {
        if (a.AuthorityBundleId is Guid bundleId)
        {
            int ord = a.AuthorityArtifactSortOrder ?? 0;

            return $"`{a.LinkId:D}` — authority bundle `{bundleId:D}`, sort order {ord.ToString(CultureInfo.InvariantCulture)}";
        }

        string hint = string.IsNullOrWhiteSpace(a.PilotArtifactHint) ? "—" : OneLine(a.PilotArtifactHint!);

        return $"`{a.LinkId:D}` — pilot hint: {hint}";
    }

    private static void AppendBulletList(StringBuilder sb, IEnumerable<string> lines)
    {
        List<string> materialized = lines.ToList();

        if (materialized.Count == 0)
        {
            sb.AppendLine("- _None listed (or truncated by export limits)._");

            return;
        }

        foreach (string line in materialized)
        {
            sb.AppendLine($"- {line}");
        }
    }

    private static string OneLine(string value) => value
        .Replace("\r\n", " ", StringComparison.Ordinal)
        .Replace('\n', ' ')
        .Replace('\r', ' ')
        .Trim();

    private static string EscapeHeading(string value) => OneLine(value);

    private static string EscapeInline(string value) => OneLine(value).Replace('`', '\'');
}
