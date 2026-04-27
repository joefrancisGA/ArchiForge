using ArchLucid.Application.Explanation;
using ArchLucid.Contracts.Explanation;
using ArchLucid.Core.Configuration;
using ArchLucid.Core.Diagnostics;
using ArchLucid.Core.Explanation;
using ArchLucid.Core.Scoping;
using ArchLucid.Decisioning.Findings;
using ArchLucid.Decisioning.Manifest;
using ArchLucid.Decisioning.Models;
using ArchLucid.Persistence.Provenance;
using ArchLucid.Persistence.Queries;
using ArchLucid.Provenance;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ArchLucid.AgentRuntime.Explanation;

/// <inheritdoc cref="IRunExplanationSummaryService" />
public sealed class RunExplanationSummaryService(
    IExplanationService explanationService,
    IDeterministicExplanationService deterministicExplanation,
    IAuthorityQueryService authorityQuery,
    IProvenanceSnapshotRepository provenanceSnapshotRepository,
    IExplanationFaithfulnessChecker explanationFaithfulnessChecker,
    IOptions<RunExplanationAggregateOptions> aggregateOptions,
    ILogger<RunExplanationSummaryService> logger) : IRunExplanationSummaryService
{
    /// <inheritdoc />
    public async Task<RunExplanationSummary?> GetSummaryAsync(ScopeContext scope, Guid runId, CancellationToken ct)
    {
        RunDetailDto? detail = await authorityQuery.GetRunDetailAsync(scope, runId, ct);
        if (detail?.GoldenManifest is null)
            return null;

        GoldenManifest manifest = detail.GoldenManifest;
        DecisionProvenanceGraph? graph = await TryLoadProvenanceGraphAsync(scope, runId, ct);
        ExplanationResult explanation = await explanationService.ExplainRunAsync(manifest, graph, ct);

        double? faithfulnessSupportRatio = null;
        bool usedDeterministicFallback = false;
        ExplanationFaithfulnessReport? lastFaithReport = null;

        if (detail.FindingsSnapshot is { Findings.Count: > 0 })
        {
            ExplanationFaithfulnessReport faithReport =
                explanationFaithfulnessChecker.CheckFaithfulness(explanation, detail.FindingsSnapshot);

            lastFaithReport = faithReport;

            if (faithReport.ClaimsChecked > 0)
            {
                faithfulnessSupportRatio = faithReport.SupportRatio;
                ArchLucidInstrumentation.RecordExplanationFaithfulnessRatio(faithReport.SupportRatio);
            }

            RunExplanationAggregateOptions aggOpts = aggregateOptions.Value;

            if (aggOpts.FaithfulnessFallbackEnabled
                && faithReport.ClaimsChecked > 0
                && faithReport.SupportRatio < aggOpts.MinSupportRatioToTrustLlmNarrative)
            {
                List<string> keyDrivers = deterministicExplanation.ExtractRunKeyDrivers(manifest, graph);
                List<string> risks = deterministicExplanation.ExtractRiskImplications(manifest);
                List<string> costs = deterministicExplanation.ExtractCostImplications(manifest);
                List<string> compliance = deterministicExplanation.ExtractComplianceImplications(manifest);

                explanation = deterministicExplanation.BuildRunExplanationFromLlmPayload(
                    manifest,
                    keyDrivers,
                    risks,
                    costs,
                    compliance,
                    string.Empty);

                explanation.Provenance = null;
                explanation.Confidence = null;
                usedDeterministicFallback = true;
                ArchLucidInstrumentation.ExplanationAggregateFaithfulnessFallbacksTotal.Add(1);

                logger.LogInformation(
                    "Aggregate explanation for run {RunId} used deterministic narrative (faithfulness support ratio {Ratio:F2} below threshold {Threshold:F2}).",
                    runId,
                    faithReport.SupportRatio,
                    aggOpts.MinSupportRatioToTrustLlmNarrative);
            }
        }

        string? faithfulnessWarning = BuildFaithfulnessWarning(lastFaithReport, usedDeterministicFallback);
        IReadOnlyList<FindingTraceConfidenceDto> findingConfidences =
            FindingTraceConfidenceMapper.FromSnapshot(detail.FindingsSnapshot);

        IReadOnlyList<CitationReference> citations = RunExplanationCitationBuilder.Build(detail);
        foreach (CitationReference c in citations)

            ArchLucidInstrumentation.ExplanationCitationsEmitted.Add(
                1,
                new KeyValuePair<string, object?>("kind", c.Kind.ToString()));


        List<string> themeSummaries = BuildThemeSummaries(explanation.KeyDrivers);
        string riskPosture = AuthorityManifestRiskPosture.Derive(manifest);
        string overallAssessment = BuildOverallAssessment(explanation, manifest, riskPosture);
        int findingCount = detail.FindingsSnapshot?.Findings.Count ?? 0;

        return new RunExplanationSummary
        {
            Explanation = explanation,
            ThemeSummaries = themeSummaries,
            OverallAssessment = overallAssessment,
            RiskPosture = riskPosture,
            FindingCount = findingCount,
            DecisionCount = manifest.Decisions.Count,
            UnresolvedIssueCount = manifest.UnresolvedIssues.Items.Count,
            ComplianceGapCount = manifest.Compliance.Gaps.Count,
            FaithfulnessSupportRatio = faithfulnessSupportRatio,
            UsedDeterministicFallback = usedDeterministicFallback,
            FaithfulnessWarning = faithfulnessWarning,
            FindingTraceConfidences = findingConfidences.Count > 0 ? findingConfidences : null,
            Citations = citations
        };
    }

    private static string? BuildFaithfulnessWarning(
        ExplanationFaithfulnessReport? faithReport,
        bool usedDeterministicFallback)
    {
        if (faithReport is null || faithReport.ClaimsChecked <= 0)
            return null;

        if (usedDeterministicFallback)

            return
                "The aggregate narrative was replaced with deterministic manifest text because AI-generated text did not sufficiently match underlying finding traces.";


        return faithReport.SupportRatio < 0.5 - 1e-9
            ? "Explanation may not fully reflect the underlying findings; review finding traces and the provenance graph."
            : null;
    }

    private async Task<DecisionProvenanceGraph?> TryLoadProvenanceGraphAsync(
        ScopeContext scope,
        Guid runId,
        CancellationToken ct)
    {
        DecisionProvenanceSnapshot? snapshot = await provenanceSnapshotRepository.GetByRunIdAsync(scope, runId, ct);
        if (snapshot is null)
            return null;

        try
        {
            return ProvenanceGraphSerializer.Deserialize(snapshot.GraphJson);
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning(
                ex,
                "Provenance graph JSON for run {RunId} is corrupt; aggregate explanation will proceed without provenance graph.",
                runId);

            return null;
        }
    }

    /// <summary>
    ///     Groups <see cref="ExplanationResult.KeyDrivers" /> lines in <c>Category: Title → Option</c> form by category
    ///     prefix.
    /// </summary>
    internal static List<string> BuildThemeSummaries(IReadOnlyList<string> keyDrivers)
    {
        Dictionary<string, List<string>> byCategory = new(StringComparer.OrdinalIgnoreCase);
        List<string> otherLines = [];

        foreach (string line in keyDrivers)
        {
            if (string.IsNullOrWhiteSpace(line))
                continue;

            if (TryParseDecisionDriverLine(line, out string category, out string titleAndOption))
            {
                if (!byCategory.TryGetValue(category, out List<string>? bucket))
                {
                    bucket = [];
                    byCategory[category] = bucket;
                }

                bucket.Add(titleAndOption);
            }
            else

                otherLines.Add(line.Trim());
        }

        List<string> themes = [];

        foreach (KeyValuePair<string, List<string>> kv in byCategory.OrderBy(k => k.Key,
                     StringComparer.OrdinalIgnoreCase))
        {
            List<string> titles = kv.Value;
            string preview = string.Join("; ", titles.Take(3));
            if (titles.Count > 3)
                preview += "…";

            themes.Add($"{kv.Key}: {titles.Count} key driver(s) — {preview}");
        }

        if (otherLines.Count > 0)
            themes.Add("Additional signals: " + string.Join(" · ", otherLines));

        if (themes.Count == 0)
            themes.Add("No categorized decision drivers extracted for this run.");

        return themes;
    }

    internal static bool TryParseDecisionDriverLine(string line, out string category, out string titleAndOption)
    {
        category = string.Empty;
        titleAndOption = string.Empty;
        int arrowIdx = line.IndexOf(" → ", StringComparison.Ordinal);
        if (arrowIdx <= 0)
            return false;

        int colonIdx = line.IndexOf(": ", StringComparison.Ordinal);
        if (colonIdx <= 0 || colonIdx >= arrowIdx)
            return false;

        category = line[..colonIdx].Trim();
        titleAndOption = line[(colonIdx + 2)..].Trim();

        return category.Length > 0;
    }

    internal static string DeriveRiskPosture(GoldenManifest manifest)
    {
        return AuthorityManifestRiskPosture.Derive(manifest);
    }

    internal static string BuildOverallAssessment(ExplanationResult explanation, GoldenManifest manifest,
        string riskPosture)
    {
        int issues = manifest.UnresolvedIssues.Items.Count;
        int gaps = manifest.Compliance.Gaps.Count;
        string summary = string.IsNullOrWhiteSpace(explanation.Summary)
            ? explanation.DetailedNarrative
            : explanation.Summary;

        if (issues == 0 && gaps == 0)
            return
                $"Overall assessment ({riskPosture} risk posture): no unresolved issues or compliance gaps on the manifest; {summary}";

        return
            $"Overall assessment ({riskPosture} risk posture): {issues} unresolved issue(s), {gaps} compliance gap(s). {summary}";
    }
}
