using System.Globalization;
using System.Text;

using ArchiForge.Api.Models.Evolution;

namespace ArchiForge.Api.Services.Evolution;

/// <summary>Renders <see cref="EvolutionSimulationReportDocument"/> as Markdown.</summary>
public static class EvolutionSimulationReportMarkdownFormatter
{
    public static string Format(EvolutionSimulationReportDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);

        StringBuilder md = new();
        md.AppendLine("# Evolution simulation report");
        md.AppendLine();
        md.AppendLine($"**Export schema:** `{document.SchemaVersion}`");
        md.AppendLine($"**Generated (UTC):** {document.GeneratedUtc:O}");
        md.AppendLine();

        AppendCandidate(md, document.Candidate);
        AppendPlanSnapshot(md, document);
        AppendRuns(md, document.SimulationRuns);

        return md.ToString();
    }

    private static void AppendCandidate(StringBuilder md, EvolutionSimulationReportCandidateSection c)
    {
        md.AppendLine("## Change set (description)");
        md.AppendLine();
        md.AppendLine($"- **Candidate ID:** `{c.CandidateChangeSetId:D}`");
        md.AppendLine($"- **Source plan ID:** `{c.SourcePlanId:D}`");
        md.AppendLine($"- **Status:** {EscapeInline(c.Status)}");
        md.AppendLine($"- **Derivation:** {EscapeInline(c.DerivationRuleVersion)}");
        md.AppendLine($"- **Created (UTC):** {c.CreatedUtc:O}");
        md.AppendLine($"- **Title:** {EscapeInline(c.Title)}");
        md.AppendLine($"- **Summary:** {EscapeInline(c.Summary)}");

        if (!string.IsNullOrWhiteSpace(c.CreatedByUserId))
        {
            md.AppendLine($"- **Created by:** {EscapeInline(c.CreatedByUserId)}");
        }

        md.AppendLine();
    }

    private static void AppendPlanSnapshot(StringBuilder md, EvolutionSimulationReportDocument document)
    {
        md.AppendLine("## Plan snapshot & expected impact");
        md.AppendLine();

        if (document.PlanSnapshot is null)
        {
            md.AppendLine("Plan snapshot JSON could not be parsed as structured fields. Raw JSON follows.");
            md.AppendLine();
            md.AppendLine("```json");
            md.AppendLine(document.PlanSnapshotJson);
            md.AppendLine("```");
            md.AppendLine();

            return;
        }

        EvolutionSimulationPlanSnapshotPayload p = document.PlanSnapshot;

        md.AppendLine($"- **Plan ID:** `{p.PlanId:D}`");
        md.AppendLine($"- **Theme ID:** `{p.ThemeId:D}`");
        md.AppendLine($"- **Plan title:** {EscapeInline(p.Title)}");
        md.AppendLine($"- **Plan summary:** {EscapeInline(p.Summary)}");
        md.AppendLine($"- **Priority score:** {p.PriorityScore.ToString(CultureInfo.InvariantCulture)}");

        if (!string.IsNullOrWhiteSpace(p.PriorityExplanation))
        {
            md.AppendLine($"- **Priority explanation (expected impact):** {EscapeInline(p.PriorityExplanation)}");
        }

        md.AppendLine($"- **Plan status:** {EscapeInline(p.Status)}");
        md.AppendLine($"- **Action step count:** {p.ActionStepCount.ToString(CultureInfo.InvariantCulture)}");
        md.AppendLine("- **Linked baseline architecture run IDs:**");

        if (p.LinkedArchitectureRunIds.Count == 0)
        {
            md.AppendLine("  - _(none)_");
        }
        else
        {
            foreach (string id in p.LinkedArchitectureRunIds)
            {
                md.AppendLine($"  - `{id}`");
            }
        }

        md.AppendLine();
    }

    private static void AppendRuns(StringBuilder md, IReadOnlyList<EvolutionSimulationReportRunEntry> runs)
    {
        md.AppendLine("## Simulation results");
        md.AppendLine();

        if (runs.Count == 0)
        {
            md.AppendLine("_No simulation rows persisted for this candidate._");
            md.AppendLine();

            return;
        }

        int i = 1;

        foreach (EvolutionSimulationReportRunEntry run in runs)
        {
            md.AppendLine($"### Run {i.ToString(CultureInfo.InvariantCulture)} — baseline `{run.BaselineArchitectureRunId}`");
            md.AppendLine();
            md.AppendLine($"- **Simulation run ID:** `{run.SimulationRunId:D}`");
            md.AppendLine($"- **Evaluation mode:** {EscapeInline(run.EvaluationMode)}");
            md.AppendLine($"- **Completed (UTC):** {run.CompletedUtc:O}");
            md.AppendLine($"- **Shadow-only:** {run.IsShadowOnly}");
            md.AppendLine($"- **Outcome schema (evaluation block):** {EscapeInline(run.OutcomeSchemaVersion ?? "—")}");
            md.AppendLine($"- **Outcome shadow kind:** {EscapeInline(run.OutcomeShadowKind)}");
            md.AppendLine();

            md.AppendLine("#### Diff summary");
            md.AppendLine();

            foreach (string line in run.DiffSummaryLines)
            {
                md.AppendLine($"- {EscapeInline(line)}");
            }

            md.AppendLine();

            md.AppendLine("#### Evaluation scores");
            md.AppendLine();

            if (run.EvaluationScore is null)
            {
                md.AppendLine("_No parsed evaluation score object for this row._");
            }
            else
            {
                EvaluationScoreResponse e = run.EvaluationScore;
                md.AppendLine("| Metric | Value |");
                md.AppendLine("| --- | --- |");
                md.AppendLine($"| Simulation | {FormatScore(e.SimulationScore)} |");
                md.AppendLine($"| Determinism | {FormatScore(e.DeterminismScore)} |");
                md.AppendLine($"| Regression risk | {FormatScore(e.RegressionRiskScore)} |");
                md.AppendLine($"| Improvement delta | {FormatScore(e.ImprovementDelta)} |");
                md.AppendLine($"| Confidence | {FormatScore(e.ConfidenceScore)} |");
                md.AppendLine();

                if (e.RegressionSignals.Count > 0)
                {
                    md.AppendLine("**Regression signals:**");
                    md.AppendLine();

                    foreach (string s in e.RegressionSignals)
                    {
                        md.AppendLine($"- {EscapeInline(s)}");
                    }

                    md.AppendLine();
                }
            }

            if (!string.IsNullOrWhiteSpace(run.EvaluationExplanationSummary))
            {
                md.AppendLine("#### Evaluation explanation (summary)");
                md.AppendLine();
                md.AppendLine(EscapeInline(run.EvaluationExplanationSummary));
                md.AppendLine();
            }

            md.AppendLine("#### Shadow outcome (structured)");
            md.AppendLine();

            if (run.ShadowOutcome is null)
            {
                md.AppendLine("_No structured shadow object._");
            }
            else
            {
                EvolutionShadowOutcomeSnapshot s = run.ShadowOutcome;
                md.AppendLine($"- **Error:** {EscapeInline(s.Error ?? "—")}");
                md.AppendLine($"- **Architecture run ID:** `{s.ArchitectureRunId}`");
                md.AppendLine($"- **Evaluation mode:** {EscapeInline(s.EvaluationMode)}");
                md.AppendLine($"- **Run status:** {EscapeInline(s.RunStatus ?? "—")}");
                md.AppendLine($"- **Manifest version:** {EscapeInline(s.ManifestVersion ?? "—")}");
                md.AppendLine($"- **Has manifest:** {s.HasManifest}");
                md.AppendLine($"- **Summary length:** {s.SummaryLength.ToString(CultureInfo.InvariantCulture)}");
                md.AppendLine($"- **Warning count:** {s.WarningCount.ToString(CultureInfo.InvariantCulture)}");
            }

            md.AppendLine();

            md.AppendLine("#### Raw outcome JSON");
            md.AppendLine();
            md.AppendLine("```json");
            md.AppendLine(run.OutcomeJson);
            md.AppendLine("```");

            if (!string.IsNullOrWhiteSpace(run.WarningsJson))
            {
                md.AppendLine();
                md.AppendLine("#### Warnings JSON");
                md.AppendLine();
                md.AppendLine("```json");
                md.AppendLine(run.WarningsJson);
                md.AppendLine("```");
            }

            md.AppendLine();
            i++;
        }
    }

    private static string FormatScore(double? value)
    {
        return value is null ? "—" : value.Value.ToString("0.####", CultureInfo.InvariantCulture);
    }

    private static string EscapeInline(string? text)
    {
        return string.IsNullOrEmpty(text) ? "—" : text.Replace("\r\n", "\n", StringComparison.Ordinal).Replace("\n", " ", StringComparison.Ordinal);
    }
}
