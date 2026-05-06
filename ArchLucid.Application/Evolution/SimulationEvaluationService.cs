using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using ArchLucid.Application.Analysis;
using ArchLucid.Application.Determinism;
using ArchLucid.Application.Diffs;
using ArchLucid.Contracts.Evolution;
using ArchLucid.Contracts.Manifest;
using JetBrains.Annotations;

namespace ArchLucid.Application.Evolution;
/// <summary>
///     Deterministic scoring over architecture analysis reports: reuses <see cref = "IManifestDiffService"/> and optional
///     <see cref = "IDeterminismCheckService"/> (live path documented; may create replay run rows).
/// </summary>
public sealed class SimulationEvaluationService(IManifestDiffService manifestDiffService, IDeterminismCheckService determinismCheckService) : ISimulationEvaluationService
{
    private readonly byte __primaryConstructorArgumentValidation = __ValidatePrimaryConstructorArguments(manifestDiffService, determinismCheckService);
    private static byte __ValidatePrimaryConstructorArguments(ArchLucid.Application.Diffs.IManifestDiffService manifestDiffService, ArchLucid.Application.Determinism.IDeterminismCheckService determinismCheckService)
    {
        ArgumentNullException.ThrowIfNull(manifestDiffService);
        ArgumentNullException.ThrowIfNull(determinismCheckService);
        return (byte)0;
    }

    private const string RuleVersion = "60R-eval-v1";
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = false
    };
    /// <inheritdoc/>
    public async Task<SimulationEvaluationResult> EvaluateAsync(SimulationEvaluationRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.BaselineReport);
        SimulationEvaluationOptions? options = request.Options;
        if (options?.InvokeLiveDeterminismCheck == true)
            if (string.IsNullOrWhiteSpace(options.BaselineArchitectureRunIdForDeterminism) && string.IsNullOrWhiteSpace(request.BaselineArchitectureRunId))
                throw new InvalidOperationException("InvokeLiveDeterminismCheck requires BaselineArchitectureRunIdForDeterminism or BaselineArchitectureRunId.");
        ArchitectureAnalysisReport baseline = request.BaselineReport;
        ArchitectureAnalysisReport? simulated = request.SimulatedReport;
        (ManifestDiffResult? diff, bool usedPrecomputedManifestDiff, bool usedComputedManifestDiff) = ResolveManifestDiff(baseline, simulated);
        DeterminismResolution determinismResolution = await ResolveDeterminismAsync(request, options, cancellationToken);
        double? regressionRisk = ComputeRegressionRiskScore(diff);
        List<string> regressionSignals = BuildRegressionSignals(diff, determinismResolution.Result);
        regressionSignals.Sort(StringComparer.Ordinal);
        double improvementDelta = ComputeImprovementDelta(baseline, simulated, diff);
        double? determinismScore = ComputeDeterminismScore(determinismResolution.Result);
        double confidenceScore = ComputeConfidenceScore(baseline, simulated, diff, determinismResolution.Result, options);
        double simulationScore = ComputeSimulationScore(improvementDelta, determinismScore, regressionRisk, confidenceScore);
        EvaluationScore score = new()
        {
            SimulationScore = simulationScore,
            DeterminismScore = determinismScore,
            RegressionRiskScore = regressionRisk,
            ImprovementDelta = improvementDelta,
            RegressionSignals = regressionSignals,
            ConfidenceScore = confidenceScore
        };
        EvaluationExplanationDto detail = new(RuleVersion, baseline.Warnings.Count, simulated?.Warnings.Count, usedPrecomputedManifestDiff, usedComputedManifestDiff, determinismResolution.Source, regressionSignals.Count);
        string detailJson = JsonSerializer.Serialize(detail, JsonOptions);
        string regressionPart = regressionRisk.HasValue ? string.Format(CultureInfo.InvariantCulture, "{0:F3}", regressionRisk.Value) : "n/a";
        string summary = string.Format(CultureInfo.InvariantCulture, "Rule={0}; ImprovementDelta={1:F3}; SimulationScore={2:F3}; Determinism={3}; RegressionRisk={4}; Confidence={5:F3}; Signals={6}", RuleVersion, improvementDelta, simulationScore, FormatDeterminism(determinismScore), regressionPart, confidenceScore, regressionSignals.Count);
        return new SimulationEvaluationResult
        {
            Score = score,
            ExplanationSummary = summary,
            ExplanationDetailJson = detailJson
        };
    }

    private static string FormatDeterminism(double? determinismScore)
    {
        return determinismScore is null ? "n/a" : string.Format(CultureInfo.InvariantCulture, "{0:F3}", determinismScore.Value);
    }

    private (ManifestDiffResult? Diff, bool UsedPrecomputed, bool UsedComputed) ResolveManifestDiff(ArchitectureAnalysisReport baseline, ArchitectureAnalysisReport? simulated)
    {
        if (simulated is null)
            return (null, false, false);
        if (simulated.ManifestDiff is not null)
            return (simulated.ManifestDiff, true, false);
        GoldenManifest? left = baseline.Manifest;
        GoldenManifest? right = simulated.Manifest;
        if (left is null || right is null)
            return (null, false, false);
        ManifestDiffResult computed = manifestDiffService.Compare(left, right);
        return (computed, false, true);
    }

    private async Task<DeterminismResolution> ResolveDeterminismAsync(SimulationEvaluationRequest request, SimulationEvaluationOptions? options, CancellationToken cancellationToken)
    {
        if (request.SuppliedDeterminism is not null)
            return new DeterminismResolution(request.SuppliedDeterminism, "Supplied");
        if (request.BaselineReport.Determinism is not null)
            return new DeterminismResolution(request.BaselineReport.Determinism, "BaselineReport");
        if (options?.InvokeLiveDeterminismCheck != true)
            return new DeterminismResolution(null, "None");
        string runId = !string.IsNullOrWhiteSpace(options.BaselineArchitectureRunIdForDeterminism) ? options.BaselineArchitectureRunIdForDeterminism.Trim() : request.BaselineArchitectureRunId!.Trim();
        int iterations = Math.Max(2, options.DeterminismIterations);
        DeterminismCheckResult live = await determinismCheckService.RunAsync(new DeterminismCheckRequest { RunId = runId, Iterations = iterations, CommitReplays = false }, cancellationToken);
        return new DeterminismResolution(live, "Live");
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

    private static List<string> BuildRegressionSignals(ManifestDiffResult? diff, DeterminismCheckResult? determinism)
    {
        List<string> signals = [];
        if (diff is not null)
        {
            AppendCountSignal(signals, "Regression.RemovedServices", diff.RemovedServices.Count);
            AppendCountSignal(signals, "Regression.RemovedDatastores", diff.RemovedDatastores.Count);
            AppendCountSignal(signals, "Regression.RemovedRelationships", diff.RemovedRelationships.Count);
            AppendCountSignal(signals, "Regression.RemovedRequiredControls", diff.RemovedRequiredControls.Count);
        }

        if (determinism is not null && !determinism.IsDeterministic)
            signals.Add("Determinism.ReplayDrift");
        return signals;
    }

    private static void AppendCountSignal(List<string> signals, string prefix, int count)
    {
        if (count > 0)
            signals.Add(string.Format(CultureInfo.InvariantCulture, "{0}:{1}", prefix, count));
    }

    private static double ComputeImprovementDelta(ArchitectureAnalysisReport baseline, ArchitectureAnalysisReport? simulated, ManifestDiffResult? diff)
    {
        int baselineWarnings = baseline.Warnings.Count;
        int simulatedWarnings = simulated?.Warnings.Count ?? baselineWarnings;
        double fromWarnings = Math.Clamp((baselineWarnings - simulatedWarnings) / 20.0, -0.5, 0.5);
        if (diff is null)
            return Math.Clamp(fromWarnings, -1, 1);
        int adds = diff.AddedServices.Count + diff.AddedDatastores.Count + diff.AddedRelationships.Count + diff.AddedRequiredControls.Count;
        int removals = diff.RemovedServices.Count + diff.RemovedDatastores.Count + diff.RemovedRelationships.Count + diff.RemovedRequiredControls.Count;
        double structural = Math.Clamp((adds - removals) / 20.0, -0.5, 0.5);
        return Math.Clamp(fromWarnings + structural, -1, 1);
    }

    private static double? ComputeDeterminismScore(DeterminismCheckResult? determinism)
    {
        if (determinism is null)
            return null;
        return determinism.IsDeterministic ? 1 : 0;
    }

    private static double ComputeConfidenceScore(ArchitectureAnalysisReport baseline, ArchitectureAnalysisReport? simulated, ManifestDiffResult? diff, DeterminismCheckResult? determinism, SimulationEvaluationOptions? options)
    {
        double confidence = 1.0;
        if (baseline.Manifest is null)
            confidence -= 0.2;
        if (simulated is null)
            confidence -= 0.15;
        if (simulated is not null && baseline.Manifest is not null && simulated.Manifest is not null && diff is null)
            confidence -= 0.1;
        if (determinism is null && options?.InvokeLiveDeterminismCheck != true)
            confidence -= 0.1;
        return Math.Clamp(confidence, 0, 1);
    }

    private static double ComputeSimulationScore(double improvementDelta, double? determinismScore, double? regressionRisk, double confidenceScore)
    {
        double normImprovement = (improvementDelta + 1.0) / 2.0;
        double det = determinismScore ?? 0.5;
        double reg = regressionRisk ?? 0;
        return Math.Clamp(normImprovement * 0.35 + det * 0.35 + (1.0 - reg) * 0.2 + confidenceScore * 0.1, 0, 1);
    }

    private sealed record DeterminismResolution(DeterminismCheckResult? Result, string Source);
    private sealed record EvaluationExplanationDto([UsedImplicitly] string RuleVersion, int BaselineWarningCount, int? SimulatedWarningCount, bool UsedPrecomputedManifestDiff, bool UsedComputedManifestDiff, string DeterminismSource, int RegressionSignalCount);
}