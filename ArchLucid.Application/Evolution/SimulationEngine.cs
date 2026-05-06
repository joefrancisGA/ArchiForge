using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using ArchLucid.Application.Analysis;
using ArchLucid.Application.Diffs;
using ArchLucid.Contracts.Abstractions.Evolution;
using ArchLucid.Contracts.Evolution;
using JetBrains.Annotations;

namespace ArchLucid.Application.Evolution;
/// <summary>
///     Read-only simulation: one or two <see cref = "IArchitectureAnalysisService.BuildAsync"/> passes with configurable
///     flags.
///     Never enables determinism checks, replay commits, or writes.
/// </summary>
public sealed class SimulationEngine(IArchitectureAnalysisService analysisService) : ISimulationEngine
{
    private readonly byte __primaryConstructorArgumentValidation = __ValidatePrimaryConstructorArguments(analysisService);
    private static byte __ValidatePrimaryConstructorArguments(ArchLucid.Application.Analysis.IArchitectureAnalysisService analysisService)
    {
        ArgumentNullException.ThrowIfNull(analysisService);
        return (byte)0;
    }

    private const int SummaryPreviewMaxChars = 512;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = false
    };
    /// <inheritdoc/>
    public async Task<SimulationResult> SimulateAsync(SimulationRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.CandidateChangeSet);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.BaselineArchitectureRunId);
        SimulationReadProfile baselineProfile = request.Options?.BaselineReadProfile ?? SimulationReadProfile.StrictReadOnly;
        SimulationReadProfile simulatedProfile = request.Options?.SimulatedReadProfile ?? baselineProfile;
        bool singlePass = baselineProfile.Equals(simulatedProfile);
        ArchitectureAnalysisReport baselineReport = await analysisService.BuildAsync(ToAnalysisRequest(request.BaselineArchitectureRunId, baselineProfile), cancellationToken);
        ArchitectureAnalysisReport simulatedReport;
        if (singlePass)
            simulatedReport = baselineReport;
        else
            simulatedReport = await analysisService.BuildAsync(ToAnalysisRequest(request.BaselineArchitectureRunId, simulatedProfile), cancellationToken);
        SimulationArtifactsSnapshot artifacts = BuildArtifacts(baselineReport, simulatedReport);
        SimulationDiff diff = BuildDiff(request.CandidateChangeSet, request.BaselineArchitectureRunId, singlePass, baselineReport, simulatedReport);
        EvaluationScore scores = BuildScores(baselineReport, simulatedReport, singlePass);
        List<string> warnings = MergeWarnings(singlePass, baselineReport, simulatedReport);
        return new SimulationResult
        {
            BaselineArchitectureRunId = request.BaselineArchitectureRunId,
            Scores = scores,
            Diff = diff,
            Artifacts = artifacts,
            Warnings = warnings,
            CompletedUtc = DateTime.UtcNow
        };
    }

    private static ArchitectureAnalysisRequest ToAnalysisRequest(string runId, SimulationReadProfile profile)
    {
        return new ArchitectureAnalysisRequest
        {
            RunId = runId,
            IncludeEvidence = profile.IncludeEvidence,
            IncludeExecutionTraces = profile.IncludeExecutionTraces,
            IncludeManifest = profile.IncludeManifest,
            IncludeDiagram = profile.IncludeDiagram,
            IncludeSummary = profile.IncludeSummary,
            IncludeDeterminismCheck = false,
            IncludeManifestCompare = profile.IncludeManifestCompare,
            CompareManifestVersion = profile.CompareManifestVersion,
            IncludeAgentResultCompare = profile.IncludeAgentResultCompare,
            CompareRunId = profile.CompareRunId
        };
    }

    private static SimulationArtifactsSnapshot BuildArtifacts(ArchitectureAnalysisReport baseline, ArchitectureAnalysisReport simulated)
    {
        string baselineVersion = baseline.Run.CurrentManifestVersion ?? string.Empty;
        string simulatedVersion = simulated.Run.CurrentManifestVersion ?? string.Empty;
        string? baselineSummary = baseline.Summary;
        string? simulatedSummary = simulated.Summary;
        return new SimulationArtifactsSnapshot
        {
            BaselineManifestVersion = baselineVersion,
            SimulatedManifestVersion = simulatedVersion,
            BaselineSummaryLength = baselineSummary?.Length ?? 0,
            SimulatedSummaryLength = simulatedSummary?.Length ?? 0,
            BaselineSummaryPreview = TruncatePreview(baselineSummary),
            SimulatedSummaryPreview = TruncatePreview(simulatedSummary)
        };
    }

    private static string? TruncatePreview(string? text)
    {
        if (string.IsNullOrEmpty(text))
            return null;
        return text.Length <= SummaryPreviewMaxChars ? text : text.Substring(0, SummaryPreviewMaxChars);
    }

    private static SimulationDiff BuildDiff(CandidateChangeSet candidate, string baselineRunId, bool singlePass, ArchitectureAnalysisReport baseline, ArchitectureAnalysisReport simulated)
    {
        ManifestDiffResult? manifestDiff = simulated.ManifestDiff;
        string summary = BuildDiffSummary(singlePass, baseline, simulated, manifestDiff);
        SimulationDiffDetailDto detail = new(candidate.ChangeSetId.ToString("D"), baselineRunId, singlePass, baseline.Warnings.Count, simulated.Warnings.Count, baseline.Summary?.Length ?? 0, simulated.Summary?.Length ?? 0, manifestDiff is not null, manifestDiff?.AddedServices.Count, manifestDiff?.RemovedServices.Count, manifestDiff?.AddedDatastores.Count, manifestDiff?.RemovedDatastores.Count);
        string detailJson = JsonSerializer.Serialize(detail, JsonOptions);
        return new SimulationDiff
        {
            Summary = summary,
            DetailJson = detailJson
        };
    }

    private static string BuildDiffSummary(bool singlePass, ArchitectureAnalysisReport baseline, ArchitectureAnalysisReport simulated, ManifestDiffResult? manifestDiff)
    {
        if (singlePass)
            return string.Format(CultureInfo.InvariantCulture, "Single read-only pass: warnings={0}, summary length={1}.", baseline.Warnings.Count, baseline.Summary?.Length ?? 0);
        string manifestLine = manifestDiff is null ? "No manifest diff in simulated pass." : string.Format(CultureInfo.InvariantCulture, "Manifest diff: +{0}/-{1} services, +{2}/-{3} datastores.", manifestDiff.AddedServices.Count, manifestDiff.RemovedServices.Count, manifestDiff.AddedDatastores.Count, manifestDiff.RemovedDatastores.Count);
        return string.Format(CultureInfo.InvariantCulture, "Baseline warnings={0}, simulated warnings={1}. {2}", baseline.Warnings.Count, simulated.Warnings.Count, manifestLine);
    }

    private static EvaluationScore BuildScores(ArchitectureAnalysisReport baseline, ArchitectureAnalysisReport simulated, bool singlePass)
    {
        double simulationScore = ComputeSimulationQualityScore(baseline, simulated, singlePass);
        double? regressionRisk = ComputeRegressionRiskScore(simulated.ManifestDiff);
        return new EvaluationScore
        {
            SimulationScore = simulationScore,
            DeterminismScore = null,
            RegressionRiskScore = regressionRisk
        };
    }

    /// <summary>Higher when simulated pass is cleaner (fewer warnings) relative to baseline; bounded [0,1].</summary>
    private static double ComputeSimulationQualityScore(ArchitectureAnalysisReport baseline, ArchitectureAnalysisReport simulated, bool singlePass)
    {
        int baselineWarnings = baseline.Warnings.Count;
        int simulatedWarnings = simulated.Warnings.Count;
        if (singlePass)
            return Math.Max(0, 1.0 - Math.Min(1.0, baselineWarnings / 20.0));
        int delta = baselineWarnings - simulatedWarnings;
        return Math.Clamp(0.5 + delta * 0.05, 0, 1);
    }

    private static double? ComputeRegressionRiskScore(ManifestDiffResult? diff)
    {
        if (diff is null)
            return null;
        int removals = diff.RemovedServices.Count + diff.RemovedDatastores.Count + diff.RemovedRelationships.Count + diff.RemovedRequiredControls.Count;
        if (removals == 0)
            return 0;
        return Math.Min(1.0, removals / 10.0);
    }

    private static List<string> MergeWarnings(bool singlePass, ArchitectureAnalysisReport baseline, ArchitectureAnalysisReport simulated)
    {
        if (singlePass)
            return[..baseline.Warnings];
        List<string> merged = [];
        foreach (string w in baseline.Warnings)
            merged.Add(string.Concat("Baseline: ", w));
        foreach (string w in simulated.Warnings)
            merged.Add(string.Concat("Simulated: ", w));
        return merged;
    }

    private sealed record SimulationDiffDetailDto([UsedImplicitly] string CandidateChangeSetId, string BaselineRunId, bool SinglePass, int BaselineWarningCount, int SimulatedWarningCount, int BaselineSummaryLength, int SimulatedSummaryLength, bool HadManifestDiff, int? ManifestAddedServices, int? ManifestRemovedServices, int? ManifestAddedDatastores, int? ManifestRemovedDatastores);
}