using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

using ArchiForge.Api.Models.Evolution;
using ArchiForge.Contracts.Evolution;

namespace ArchiForge.Api.Services.Evolution;

/// <summary>Builds <see cref="EvolutionSimulationReportDocument"/> from persisted evolution rows.</summary>
public static class EvolutionSimulationReportBuilder
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public static EvolutionSimulationReportDocument Build(
        EvolutionCandidateChangeSetRecord candidate,
        IReadOnlyList<EvolutionSimulationRunRecord> runs,
        DateTime generatedUtc)
    {
        ArgumentNullException.ThrowIfNull(candidate);
        ArgumentNullException.ThrowIfNull(runs);

        EvolutionSimulationPlanSnapshotPayload? planSnapshot = TryDeserializePlanSnapshot(candidate.PlanSnapshotJson);

        EvolutionSimulationReportCandidateSection candidateSection = new()
        {
            CandidateChangeSetId = candidate.CandidateChangeSetId,
            SourcePlanId = candidate.SourcePlanId,
            Status = candidate.Status,
            Title = candidate.Title,
            Summary = candidate.Summary,
            DerivationRuleVersion = candidate.DerivationRuleVersion,
            CreatedUtc = candidate.CreatedUtc,
            CreatedByUserId = candidate.CreatedByUserId,
        };

        IReadOnlyList<string> linked = planSnapshot?.LinkedArchitectureRunIds ?? [];

        List<EvolutionSimulationReportRunEntry> entries = [];

        foreach (EvolutionSimulationRunRecord run in runs
                     .OrderBy(static r => r.CompletedUtc)
                     .ThenBy(static r => r.SimulationRunId))
        {
            EvolutionSimulationRunWithEvaluationResponse parsed = EvolutionOutcomeParser.ToRunWithEvaluation(run);
            EvolutionOutcomeShadowReader.TryReadShadow(run.OutcomeJson, out EvolutionShadowOutcomeSnapshot? shadow, out string shadowKind);

            IReadOnlyList<string> diffLines = BuildDiffSummaryLines(
                run.BaselineArchitectureRunId,
                linked,
                shadow,
                parsed.EvaluationScore,
                shadowKind);

            entries.Add(
                new EvolutionSimulationReportRunEntry
                {
                    SimulationRunId = run.SimulationRunId,
                    BaselineArchitectureRunId = run.BaselineArchitectureRunId,
                    EvaluationMode = run.EvaluationMode,
                    CompletedUtc = run.CompletedUtc,
                    IsShadowOnly = run.IsShadowOnly,
                    OutcomeSchemaVersion = parsed.OutcomeSchemaVersion,
                    WarningsJson = run.WarningsJson,
                    OutcomeJson = run.OutcomeJson,
                    OutcomeShadowKind = shadowKind,
                    ShadowOutcome = shadow,
                    EvaluationScore = parsed.EvaluationScore,
                    EvaluationExplanationSummary = parsed.EvaluationExplanationSummary,
                    DiffSummaryLines = diffLines,
                });
        }

        return new EvolutionSimulationReportDocument
        {
            GeneratedUtc = generatedUtc,
            Candidate = candidateSection,
            PlanSnapshotJson = candidate.PlanSnapshotJson,
            PlanSnapshot = planSnapshot,
            SimulationRuns = entries,
        };
    }

    private static EvolutionSimulationPlanSnapshotPayload? TryDeserializePlanSnapshot(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<EvolutionSimulationPlanSnapshotPayload>(json, JsonOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static IReadOnlyList<string> BuildDiffSummaryLines(
        string baselineArchitectureRunId,
        IReadOnlyList<string> planLinkedRunIds,
        EvolutionShadowOutcomeSnapshot? shadow,
        EvaluationScoreResponse? evaluation,
        string shadowKind)
    {
        List<string> lines = [];

        bool onPlan = planLinkedRunIds.Contains(baselineArchitectureRunId, StringComparer.Ordinal);
        lines.Add(
            $"Before: baseline run `{baselineArchitectureRunId}` appears on the plan snapshot linked-run list: {(onPlan ? "yes" : "no")}.");

        if (planLinkedRunIds.Count > 0)
        {
            lines.Add($"Before: plan snapshot links {planLinkedRunIds.Count} baseline architecture run id(s).");
        }

        if (string.Equals(shadowKind, "none", StringComparison.Ordinal) ||
            string.Equals(shadowKind, "invalid", StringComparison.Ordinal) ||
            string.Equals(shadowKind, "unparsed", StringComparison.Ordinal))
        {
            lines.Add($"After: could not read shadow metrics from outcome JSON (parse kind: {shadowKind}).");
            AppendEvaluationSummaryLines(lines, evaluation);

            return lines;
        }

        if (shadow is null)
        {
            lines.Add("After: shadow payload was absent after parse.");
            AppendEvaluationSummaryLines(lines, evaluation);

            return lines;
        }

        if (!string.IsNullOrWhiteSpace(shadow.Error))
        {
            lines.Add($"After: shadow recorded error — {shadow.Error}");
        }
        else
        {
            lines.Add(
                string.Format(
                    CultureInfo.InvariantCulture,
                    "After: run status `{0}`; manifest version `{1}`; has manifest: {2}; summary length: {3}; analysis warnings: {4}.",
                    shadow.RunStatus ?? "—",
                    shadow.ManifestVersion ?? "—",
                    shadow.HasManifest,
                    shadow.SummaryLength,
                    shadow.WarningCount));
        }

        AppendEvaluationSummaryLines(lines, evaluation);

        return lines;
    }

    private static void AppendEvaluationSummaryLines(List<string> lines, EvaluationScoreResponse? evaluation)
    {
        if (evaluation is null)
        {
            lines.Add("Evaluation: no structured score block in this outcome (legacy flat JSON or missing evaluation).");

            return;
        }

        lines.Add(
            string.Format(
                CultureInfo.InvariantCulture,
                "Evaluation: simulation {0}; determinism {1}; regression risk {2}; improvement delta {3}; confidence {4}.",
                FormatNullableDouble(evaluation.SimulationScore),
                FormatNullableDouble(evaluation.DeterminismScore),
                FormatNullableDouble(evaluation.RegressionRiskScore),
                FormatNullableDouble(evaluation.ImprovementDelta),
                FormatNullableDouble(evaluation.ConfidenceScore)));

        if (evaluation.RegressionSignals.Count > 0)
        {
            lines.Add($"Evaluation: regression signals — {string.Join("; ", evaluation.RegressionSignals)}");
        }
    }

    private static string FormatNullableDouble(double? value)
    {
        return value is null ? "—" : value.Value.ToString("0.####", CultureInfo.InvariantCulture);
    }
}
