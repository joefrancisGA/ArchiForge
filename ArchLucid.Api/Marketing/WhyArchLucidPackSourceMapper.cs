using System.Globalization;
using System.Text;

using System.Linq;

using ArchLucid.Application.Pilots;
using ArchLucid.Contracts.Explanation;
using ArchLucid.Core.Explanation;
using ArchLucid.Host.Core.Demo;

namespace ArchLucid.Api.Marketing;

/// <summary>
/// Maps the anonymous demo preview bundle into <see cref="WhyArchLucidPackSourceDto"/> fragments for
/// <see cref="WhyArchLucidPackBuilder"/> — keeps <c>ArchLucid.Application</c> free of Host.Core demo DTOs.
/// </summary>
internal static class WhyArchLucidPackSourceMapper
{
    internal static WhyArchLucidPackSourceDto Map(DemoCommitPagePreviewResponse preview)
    {
        if (preview is null)
            throw new ArgumentNullException(nameof(preview));

        DemoPreviewRun run = preview.Run;
        DemoPreviewManifestSummary m = preview.Manifest;
        DemoPreviewAuthorityChain chain = preview.AuthorityChain;

        string manifestMd = string.Join(
            '\n',
            new[]
            {
                $"| Field | Value |",
                $"|-------|-------|",
                $"| ManifestId | `{m.ManifestId}` |",
                $"| RunId | `{m.RunId}` |",
                $"| CreatedUtc | {m.CreatedUtc:O} |",
                $"| ManifestHash | `{m.ManifestHash}` |",
                $"| RuleSetId | `{m.RuleSetId}` |",
                $"| RuleSetVersion | `{m.RuleSetVersion}` |",
                $"| DecisionCount | {m.DecisionCount.ToString(CultureInfo.InvariantCulture)} |",
                $"| WarningCount | {m.WarningCount.ToString(CultureInfo.InvariantCulture)} |",
                $"| UnresolvedIssueCount | {m.UnresolvedIssueCount.ToString(CultureInfo.InvariantCulture)} |",
                $"| Status | {m.Status} |",
                $"| HasWarnings | {m.HasWarnings.ToString()} |",
                $"| HasUnresolvedIssues | {m.HasUnresolvedIssues.ToString()} |",
                string.Empty,
                "**Operator summary (excerpt)**",
                string.Empty,
                m.OperatorSummary.Trim(),
            });

        string authorityMd = string.Join(
            '\n',
            new[]
            {
                $"| Chain id | Value |",
                $"|----------|-------|",
                Row("ContextSnapshotId", chain.ContextSnapshotId),
                Row("GraphSnapshotId", chain.GraphSnapshotId),
                Row("FindingsSnapshotId", chain.FindingsSnapshotId),
                Row("GoldenManifestId", chain.GoldenManifestId),
                Row("DecisionTraceId", chain.DecisionTraceId),
                Row("ArtifactBundleId", chain.ArtifactBundleId),
            });

        StringBuilder artifacts = new();
        artifacts.AppendLine("| Artifact | Type | Format | CreatedUtc | ContentHash |");
        artifacts.AppendLine("|----------|------|--------|------------|-------------|");

        foreach (DemoPreviewArtifact a in preview.Artifacts)
        {
            artifacts.AppendLine(
                $"| {Md(a.Name)} | {Md(a.ArtifactType)} | {Md(a.Format)} | {a.CreatedUtc:O} | `{a.ContentHash}` |");
        }

        StringBuilder timeline = new();
        timeline.AppendLine("| OccurredUtc | EventType | Actor |");
        timeline.AppendLine("|-------------|-----------|-------|");

        foreach (DemoPreviewTimelineItem item in preview.PipelineTimeline)
        {
            timeline.AppendLine(
                $"| {item.OccurredUtc:O} | {Md(item.EventType)} | {Md(item.ActorUserName)} |");
        }

        RunExplanationSummary explain = preview.RunExplanation;
        ExplanationResult ex = explain.Explanation;

        StringBuilder explainBody = new();
        explainBody.AppendLine("**Summary**");
        explainBody.AppendLine();
        explainBody.AppendLine(ex.Summary.Trim());
        explainBody.AppendLine();
        explainBody.AppendLine("**Overall assessment**");
        explainBody.AppendLine();
        explainBody.AppendLine(explain.OverallAssessment.Trim());
        explainBody.AppendLine();
        explainBody.AppendLine($"**Risk posture:** {explain.RiskPosture}");
        explainBody.AppendLine();
        explainBody.AppendLine(
            $"**Counts:** findings={explain.FindingCount.ToString(CultureInfo.InvariantCulture)}, decisions={explain.DecisionCount.ToString(CultureInfo.InvariantCulture)}, unresolved={explain.UnresolvedIssueCount.ToString(CultureInfo.InvariantCulture)}, compliance gaps={explain.ComplianceGapCount.ToString(CultureInfo.InvariantCulture)}");
        explainBody.AppendLine();

        if (explain.ThemeSummaries.Count > 0)
        {
            explainBody.AppendLine("**Theme summaries**");
            explainBody.AppendLine();

            foreach (string theme in explain.ThemeSummaries)
                explainBody.AppendLine($"- {theme.Trim()}");

            explainBody.AppendLine();
        }

        if (ex.KeyDrivers.Count > 0)
        {
            explainBody.AppendLine("**Key drivers (excerpt)**");
            explainBody.AppendLine();

            int cap = Math.Min(6, ex.KeyDrivers.Count);

            for (int i = 0; i < cap; i++)
                explainBody.AppendLine($"- {ex.KeyDrivers[i].Trim()}");

            explainBody.AppendLine();
        }

        StringBuilder citations = new();

        if (explain.Citations.Count == 0)
        {
            citations.AppendLine("_No citation rows on this demo snapshot._");
        }
        else
        {
            citations.AppendLine("| Kind | Label | Id | RunId |");
            citations.AppendLine("|------|-------|----|-------|");

            foreach (CitationReference c in explain.Citations)
            {
                string runPart = c.RunId is { } rid ? $"`{rid:N}`" : "—";
                citations.AppendLine($"| {c.Kind} | {Md(c.Label)} | `{c.Id}` | {runPart} |");
            }
        }

        StringBuilder delta = new();
        delta.AppendLine(
            "Illustrative **theme-level** delta sample from the same aggregate explanation (not a full comparison run):");
        delta.AppendLine();

        if (explain.ThemeSummaries.Count > 0)
        {
            foreach (string t in explain.ThemeSummaries.Take(4))
                delta.AppendLine($"- {t.Trim()}");
        }
        else
        {
            delta.AppendLine("_No theme summaries on this demo snapshot._");
        }

        delta.AppendLine();

        if (explain.FindingTraceConfidences is { Count: > 0 } ftc)
        {
            delta.AppendLine("**Finding trace completeness (first rows)**");
            delta.AppendLine();

            foreach (FindingTraceConfidenceDto row in ftc.Take(5))
            {
                delta.AppendLine(
                    $"- Finding `{row.FindingId}` — ratio {row.TraceCompletenessRatio.ToString(CultureInfo.InvariantCulture)} ({row.TraceConfidenceLabel})");
            }
        }

        return new WhyArchLucidPackSourceDto(
            run.RunId,
            run.ProjectId,
            manifestMd,
            authorityMd,
            artifacts.ToString(),
            timeline.ToString(),
            explainBody.ToString(),
            citations.ToString(),
            delta.ToString());
    }

    private static string Row(string label, string? value) =>
        $"| {label} | {(string.IsNullOrWhiteSpace(value) ? "—" : $"`{value}`")} |";

    private static string Md(string value) => value.Replace("|", "\\|", StringComparison.Ordinal);
}
