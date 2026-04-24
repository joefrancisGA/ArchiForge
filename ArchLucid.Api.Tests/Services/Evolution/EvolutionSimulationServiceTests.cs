using System.Text.Json;
using System.Text.Json.Serialization;

using ArchLucid.Api.Services.Evolution;
using ArchLucid.Application.Analysis;
using ArchLucid.Application.Evolution;
using ArchLucid.Contracts.Evolution;
using ArchLucid.Contracts.ProductLearning;
using ArchLucid.Contracts.ProductLearning.Planning;
using ArchLucid.Persistence.Coordination.Evolution;
using ArchLucid.Persistence.Coordination.ProductLearning.Planning;

using FluentAssertions;

using Moq;

namespace ArchLucid.Api.Tests.Services.Evolution;

/// <summary>
///     Unit coverage for <see cref="EvolutionSimulationService" /> branches that do not require a hosted API or SQL.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Suite", "Core")]
public sealed class EvolutionSimulationServiceTests
{
    private static readonly JsonSerializerOptions SnapshotJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = false
    };

    [Fact]
    public async Task CreateCandidateFromImprovementPlanAsync_throws_when_plan_not_found()
    {
        Mock<IProductLearningPlanningRepository> planning = new();
        planning
            .Setup(p => p.GetPlanAsync(It.IsAny<Guid>(), It.IsAny<ProductLearningScope>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult<ProductLearningImprovementPlanRecord?>(null));

        Mock<IEvolutionCandidateChangeSetRepository> candidates = new();
        Mock<IEvolutionSimulationRunRepository> runs = new();
        Mock<IArchitectureAnalysisService> analysis = new();
        Mock<ISimulationEvaluationService> evaluation = new();

        EvolutionSimulationService sut = new(
            planning.Object,
            candidates.Object,
            runs.Object,
            analysis.Object,
            evaluation.Object);

        ProductLearningScope scope = new()
        {
            TenantId = Guid.NewGuid(), WorkspaceId = Guid.NewGuid(), ProjectId = Guid.NewGuid()
        };

        Guid planId = Guid.NewGuid();

        Func<Task> act = () => sut.CreateCandidateFromImprovementPlanAsync(planId, scope, null, CancellationToken.None);

        EvolutionResourceNotFoundException ex =
            (await act.Should().ThrowAsync<EvolutionResourceNotFoundException>()).Which;

        ex.ProblemTypeUri.Should().Be(ProblemTypes.LearningImprovementPlanNotFound);
        planning.Verify(
            p => p.GetPlanAsync(planId, scope, It.IsAny<CancellationToken>()),
            Times.Once);
        candidates.Verify(
            c => c.InsertAsync(It.IsAny<EvolutionCandidateChangeSetRecord>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task RunShadowEvaluationAsync_with_empty_linked_run_ids_marks_simulated_and_skips_analysis()
    {
        Guid candidateId = Guid.NewGuid();
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
            ActionStepCount = 0,
            LinkedArchitectureRunIds = []
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
        Mock<IArchitectureAnalysisService> analysis = new();
        Mock<ISimulationEvaluationService> evaluation = new();

        EvolutionSimulationService sut = new(
            planning.Object,
            candidates.Object,
            runs.Object,
            analysis.Object,
            evaluation.Object);

        IReadOnlyList<EvolutionSimulationRunRecord> result =
            await sut.RunShadowEvaluationAsync(candidateId, scope, CancellationToken.None);

        result.Should().BeEmpty();
        candidates.Verify(
            c => c.UpdateStatusAsync(
                candidateId,
                scope,
                EvolutionCandidateChangeSetStatusValues.Simulated,
                It.IsAny<CancellationToken>()),
            Times.Once);
        runs.Verify(r => r.DeleteByCandidateAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        runs.Verify(r => r.InsertAsync(It.IsAny<EvolutionSimulationRunRecord>(), It.IsAny<CancellationToken>()),
            Times.Never);
        analysis.Verify(
            a => a.BuildAsync(It.IsAny<ArchitectureAnalysisRequest>(), It.IsAny<CancellationToken>()),
            Times.Never);
        evaluation.Verify(
            e => e.EvaluateAsync(It.IsAny<SimulationEvaluationRequest>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
