using System.Text.Json;
using System.Text.Json.Serialization;

using ArchLucid.Api.Services.Evolution;
using ArchLucid.Application.Analysis;
using ArchLucid.Application.Evolution;
using ArchLucid.Contracts.Common;
using ArchLucid.Contracts.Evolution;
using ArchLucid.Contracts.Manifest;
using ArchLucid.Contracts.Metadata;
using ArchLucid.Contracts.ProductLearning;
using ArchLucid.Persistence.Coordination.Evolution;
using ArchLucid.Persistence.Coordination.ProductLearning.Planning;

using FluentAssertions;

using Moq;

namespace ArchLucid.Api.Tests.Services.Evolution;

/// <summary>
///     Happy-path <see cref="EvolutionSimulationService.SimulateCandidateWithEvaluationAsync" /> with linked architecture
///     run ids,
///     including evaluation envelope serialization and prior simulation row deletion.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Suite", "Core")]
public sealed class EvolutionSimulationServiceEvaluateLinkedRunsHappyPathTests
{
    private static readonly JsonSerializerOptions SnapshotJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = false
    };

    [Fact]
    public async Task SimulateCandidateWithEvaluationAsync_runs_analysis_evaluation_inserts_rows_and_marks_simulated()
    {
        Guid candidateId = Guid.NewGuid();
        string baselineRunId = Guid.NewGuid().ToString("N");
        ProductLearningScope scope = new()
        {
            TenantId = Guid.NewGuid(), WorkspaceId = Guid.NewGuid(), ProjectId = Guid.NewGuid()
        };

        EvolutionPlanSnapshotDocument snapshot = new()
        {
            PlanId = Guid.NewGuid(),
            ThemeId = Guid.NewGuid(),
            Title = "t",
            Summary = "s",
            PriorityScore = 1,
            Status = "Open",
            ActionStepCount = 1,
            LinkedArchitectureRunIds = [baselineRunId]
        };

        string snapshotJson = JsonSerializer.Serialize(snapshot, SnapshotJsonOptions);

        EvolutionCandidateChangeSetRecord candidate = new()
        {
            CandidateChangeSetId = candidateId,
            TenantId = scope.TenantId,
            WorkspaceId = scope.WorkspaceId,
            ProjectId = scope.ProjectId,
            SourcePlanId = Guid.NewGuid(),
            Status = EvolutionCandidateChangeSetStatusValues.Draft,
            Title = "t",
            Summary = "s",
            PlanSnapshotJson = snapshotJson,
            DerivationRuleVersion = "60R-v1",
            CreatedUtc = DateTime.UtcNow
        };

        ArchitectureAnalysisReport report = new()
        {
            Run = new ArchitectureRun
            {
                RunId = baselineRunId, Status = ArchitectureRunStatus.Committed, CurrentManifestVersion = "v1"
            },
            Manifest = new GoldenManifest(),
            Summary = "ok",
            Warnings = ["w1"]
        };

        Mock<IProductLearningPlanningRepository> planning = new();
        Mock<IEvolutionCandidateChangeSetRepository> candidates = new();
        candidates
            .Setup(c => c.GetByIdAsync(candidateId, scope, It.IsAny<CancellationToken>()))
            .ReturnsAsync(candidate);
        candidates
            .Setup(c => c.UpdateStatusAsync(
                It.IsAny<Guid>(),
                It.IsAny<ProductLearningScope>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        Mock<IEvolutionSimulationRunRepository> runs = new();
        runs
            .Setup(r => r.InsertAsync(It.IsAny<EvolutionSimulationRunRecord>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        Mock<IArchitectureAnalysisService> analysis = new();
        analysis
            .Setup(a => a.BuildAsync(It.IsAny<ArchitectureAnalysisRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(report);

        Mock<ISimulationEvaluationService> evaluation = new();
        evaluation
            .Setup(e => e.EvaluateAsync(It.IsAny<SimulationEvaluationRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                new SimulationEvaluationResult
                {
                    Score = new EvaluationScore { SimulationScore = 0.9d, ConfidenceScore = 0.8d },
                    ExplanationSummary = "summary",
                    ExplanationDetailJson = "{}"
                });

        EvolutionSimulationService sut = new(
            planning.Object,
            candidates.Object,
            runs.Object,
            analysis.Object,
            evaluation.Object);

        IReadOnlyList<EvolutionSimulationRunRecord> result =
            await sut.SimulateCandidateWithEvaluationAsync(candidateId, scope, CancellationToken.None);

        result.Should().ContainSingle();
        result[0].BaselineArchitectureRunId.Should().Be(baselineRunId);
        result[0].OutcomeJson.Should().Contain("60R-v2");
        result[0].WarningsJson.Should().NotBeNullOrWhiteSpace();

        runs.Verify(r => r.DeleteByCandidateAsync(candidateId, It.IsAny<CancellationToken>()), Times.Once);
        runs.Verify(r => r.InsertAsync(It.IsAny<EvolutionSimulationRunRecord>(), It.IsAny<CancellationToken>()),
            Times.Once);
        analysis.Verify(
            a => a.BuildAsync(
                It.Is<ArchitectureAnalysisRequest>(req => req.RunId == baselineRunId),
                It.IsAny<CancellationToken>()),
            Times.Once);
        evaluation.Verify(
            e => e.EvaluateAsync(It.IsAny<SimulationEvaluationRequest>(), It.IsAny<CancellationToken>()),
            Times.Once);
        candidates.Verify(
            c => c.UpdateStatusAsync(
                candidateId,
                scope,
                EvolutionCandidateChangeSetStatusValues.Simulated,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
