using System.Text.Json;
using System.Text.Json.Serialization;

using ArchiForge.Application;
using ArchiForge.Application.Analysis;
using ArchiForge.Application.Evolution;
using ArchiForge.Contracts.Evolution;
using ArchiForge.Contracts.ProductLearning;
using ArchiForge.Contracts.ProductLearning.Planning;
using ArchiForge.Persistence.Evolution;
using ArchiForge.Persistence.ProductLearning.Planning;

namespace ArchiForge.Api.Services.Evolution;

/// <summary>
/// Builds 60R candidates from persisted 59R plans and runs shadow evaluation via read-only architecture analysis only
/// (no replay commits, no manifest writes, no agent re-execution through this path).
/// </summary>
public sealed class EvolutionSimulationService(
    IProductLearningPlanningRepository planningRepository,
    IEvolutionCandidateChangeSetRepository candidateRepository,
    IEvolutionSimulationRunRepository simulationRunRepository,
    IArchitectureAnalysisService architectureAnalysisService,
    ISimulationEvaluationService simulationEvaluationService)
    : IEvolutionSimulationService
{
    private const string DerivationRuleVersion = "60R-v1";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = false,
    };

    /// <inheritdoc />
    public async Task<EvolutionCandidateChangeSetRecord> CreateCandidateFromImprovementPlanAsync(
        Guid planId,
        ProductLearningScope scope,
        string? createdByUserId,
        CancellationToken cancellationToken)
    {
        ProductLearningImprovementPlanRecord? plan =
            await planningRepository.GetPlanAsync(planId, scope, cancellationToken);

        if (plan is null)
        {
            throw new EvolutionResourceNotFoundException(
                ProblemTypes.LearningImprovementPlanNotFound,
                $"Improvement plan '{planId}' was not found in the current scope.");
        }

        IReadOnlyList<string> runIds =
            await planningRepository.ListPlanArchitectureRunIdsAsync(planId, scope, cancellationToken);

        List<string> sortedRunIds = runIds.OrderBy(static id => id, StringComparer.Ordinal).ToList();

        EvolutionPlanSnapshotDocument snapshot = new()
        {
            PlanId = plan.PlanId,
            ThemeId = plan.ThemeId,
            Title = plan.Title,
            Summary = plan.Summary,
            PriorityScore = plan.PriorityScore,
            PriorityExplanation = plan.PriorityExplanation,
            Status = plan.Status,
            ActionStepCount = plan.ActionSteps.Count,
            LinkedArchitectureRunIds = sortedRunIds,
        };

        string snapshotJson = JsonSerializer.Serialize(snapshot, JsonOptions);
        DateTime createdUtc = DateTime.UtcNow;
        Guid candidateId = Guid.NewGuid();

        EvolutionCandidateChangeSetRecord record = new()
        {
            CandidateChangeSetId = candidateId,
            TenantId = scope.TenantId,
            WorkspaceId = scope.WorkspaceId,
            ProjectId = scope.ProjectId,
            SourcePlanId = planId,
            Status = EvolutionCandidateChangeSetStatusValues.Draft,
            Title = plan.Title,
            Summary = plan.Summary,
            PlanSnapshotJson = snapshotJson,
            DerivationRuleVersion = DerivationRuleVersion,
            CreatedUtc = createdUtc,
            CreatedByUserId = createdByUserId,
        };

        await candidateRepository.InsertAsync(record, cancellationToken);

        return record;
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<EvolutionSimulationRunRecord>> RunShadowEvaluationAsync(
        Guid candidateChangeSetId,
        ProductLearningScope scope,
        CancellationToken cancellationToken)
    {
        return RunSimulationAsync(
            candidateChangeSetId,
            scope,
            deleteExistingRunsForCandidate: false,
            useEvaluationEnvelope: false,
            cancellationToken);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<EvolutionSimulationRunRecord>> SimulateCandidateWithEvaluationAsync(
        Guid candidateChangeSetId,
        ProductLearningScope scope,
        CancellationToken cancellationToken)
    {
        return RunSimulationAsync(
            candidateChangeSetId,
            scope,
            deleteExistingRunsForCandidate: true,
            useEvaluationEnvelope: true,
            cancellationToken);
    }

    private async Task<IReadOnlyList<EvolutionSimulationRunRecord>> RunSimulationAsync(
        Guid candidateChangeSetId,
        ProductLearningScope scope,
        bool deleteExistingRunsForCandidate,
        bool useEvaluationEnvelope,
        CancellationToken cancellationToken)
    {
        EvolutionCandidateChangeSetRecord? candidate =
            await candidateRepository.GetByIdAsync(candidateChangeSetId, scope, cancellationToken);

        if (candidate is null)
        {
            throw new EvolutionResourceNotFoundException(
                ProblemTypes.EvolutionCandidateChangeSetNotFound,
                $"Candidate change set '{candidateChangeSetId}' was not found in the current scope.");
        }

        EvolutionPlanSnapshotDocument? snapshot =
            JsonSerializer.Deserialize<EvolutionPlanSnapshotDocument>(candidate.PlanSnapshotJson, JsonOptions);

        if (snapshot is null)
        {
            throw new InvalidOperationException("Stored plan snapshot is invalid JSON.");
        }

        if (deleteExistingRunsForCandidate)
        {
            await simulationRunRepository.DeleteByCandidateAsync(candidateChangeSetId, cancellationToken);
        }

        List<EvolutionSimulationRunRecord> inserted = [];
        DateTime completedUtcBase = DateTime.UtcNow;

        if (snapshot.LinkedArchitectureRunIds.Count == 0)
        {
            await candidateRepository.UpdateStatusAsync(
                candidateChangeSetId,
                scope,
                EvolutionCandidateChangeSetStatusValues.Simulated,
                cancellationToken);

            return inserted;
        }

        int ordinal = 0;

        foreach (string runId in snapshot.LinkedArchitectureRunIds)
        {
            cancellationToken.ThrowIfCancellationRequested();

            DateTime completedUtc = completedUtcBase.AddTicks(ordinal);
            ordinal++;

            (ShadowOutcomeDto outcome, IReadOnlyList<string> analysisWarnings, ArchitectureAnalysisReport? report) =
                await EvaluateRunReadOnlyAsync(runId, cancellationToken);

            string? warningsJson = analysisWarnings.Count > 0
                ? JsonSerializer.Serialize(analysisWarnings, JsonOptions)
                : null;

            string outcomeJson = useEvaluationEnvelope
                ? await BuildOutcomeEnvelopeJsonAsync(outcome, report, runId, cancellationToken)
                : JsonSerializer.Serialize(outcome, JsonOptions);

            EvolutionSimulationRunRecord row = await InsertSimulationRowAsync(
                candidateChangeSetId,
                runId,
                outcomeJson,
                warningsJson,
                completedUtc,
                cancellationToken);

            inserted.Add(row);
        }

        await candidateRepository.UpdateStatusAsync(
            candidateChangeSetId,
            scope,
            EvolutionCandidateChangeSetStatusValues.Simulated,
            cancellationToken);

        return inserted;
    }

    private async Task<(ShadowOutcomeDto Outcome, IReadOnlyList<string> Warnings, ArchitectureAnalysisReport? Report)>
        EvaluateRunReadOnlyAsync(
            string runId,
            CancellationToken cancellationToken)
    {
        ArchitectureAnalysisRequest request = new()
        {
            RunId = runId,
            IncludeEvidence = false,
            IncludeExecutionTraces = false,
            IncludeManifest = true,
            IncludeDiagram = false,
            IncludeSummary = true,
            IncludeDeterminismCheck = false,
            IncludeManifestCompare = false,
            IncludeAgentResultCompare = false,
        };

        try
        {
            ArchitectureAnalysisReport report =
                await architectureAnalysisService.BuildAsync(request, cancellationToken);

            ShadowOutcomeDto outcome = new(
                Error: null,
                ArchitectureRunId: runId,
                EvaluationMode: EvolutionEvaluationModeValues.ReadOnlyArchitectureAnalysis,
                RunStatus: report.Run.Status.ToString(),
                ManifestVersion: report.Run.CurrentManifestVersion,
                HasManifest: report.Manifest is not null,
                SummaryLength: report.Summary?.Length ?? 0,
                WarningCount: report.Warnings.Count);

            return (outcome, report.Warnings, report);
        }
        catch (RunNotFoundException)
        {
            ShadowOutcomeDto outcome = new(
                Error: $"Run '{runId}' was not found.",
                ArchitectureRunId: runId,
                EvaluationMode: EvolutionEvaluationModeValues.ReadOnlyArchitectureAnalysis,
                RunStatus: null,
                ManifestVersion: null,
                HasManifest: false,
                SummaryLength: 0,
                WarningCount: 0);

            return (outcome, [], null);
        }
    }

    private async Task<string> BuildOutcomeEnvelopeJsonAsync(
        ShadowOutcomeDto shadow,
        ArchitectureAnalysisReport? report,
        string runId,
        CancellationToken cancellationToken)
    {
        EvaluationScore? evaluationScore = null;
        string? explanationSummary = null;
        string? explanationDetailJson = null;

        if (report is not null)
        {
            SimulationEvaluationResult evaluationResult =
                await simulationEvaluationService.EvaluateAsync(
                    new SimulationEvaluationRequest
                    {
                        BaselineReport = report,
                        BaselineArchitectureRunId = runId,
                    },
                    cancellationToken);

            evaluationScore = evaluationResult.Score;
            explanationSummary = evaluationResult.ExplanationSummary;
            explanationDetailJson = evaluationResult.ExplanationDetailJson;
        }

        EvolutionOutcomeEnvelopeV2 envelope = new(
            SchemaVersion: "60R-v2",
            Shadow: shadow,
            Evaluation: evaluationScore,
            ExplanationSummary: explanationSummary,
            ExplanationDetailJson: explanationDetailJson);

        return JsonSerializer.Serialize(envelope, JsonOptions);
    }

    private async Task<EvolutionSimulationRunRecord> InsertSimulationRowAsync(
        Guid candidateChangeSetId,
        string baselineRunId,
        string outcomeJson,
        string? warningsJson,
        DateTime completedUtc,
        CancellationToken cancellationToken)
    {
        EvolutionSimulationRunRecord record = new()
        {
            SimulationRunId = Guid.NewGuid(),
            CandidateChangeSetId = candidateChangeSetId,
            BaselineArchitectureRunId = baselineRunId,
            EvaluationMode = EvolutionEvaluationModeValues.ReadOnlyArchitectureAnalysis,
            OutcomeJson = outcomeJson,
            WarningsJson = warningsJson,
            CompletedUtc = completedUtc,
            IsShadowOnly = true,
        };

        await simulationRunRepository.InsertAsync(record, cancellationToken);

        return record;
    }

    private sealed record ShadowOutcomeDto(
        string? Error,
        string ArchitectureRunId,
        string EvaluationMode,
        string? RunStatus,
        string? ManifestVersion,
        bool HasManifest,
        int SummaryLength,
        int WarningCount);

    private sealed record EvolutionOutcomeEnvelopeV2(
        [property: JsonPropertyName("schemaVersion")] string SchemaVersion,
        [property: JsonPropertyName("shadow")] ShadowOutcomeDto Shadow,
        [property: JsonPropertyName("evaluation")] EvaluationScore? Evaluation,
        [property: JsonPropertyName("explanationSummary")] string? ExplanationSummary,
        [property: JsonPropertyName("explanationDetailJson")] string? ExplanationDetailJson);
}
