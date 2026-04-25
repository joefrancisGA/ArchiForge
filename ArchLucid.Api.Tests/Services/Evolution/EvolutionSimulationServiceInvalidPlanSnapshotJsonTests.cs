using ArchLucid.Api.Services.Evolution;
using ArchLucid.Application.Analysis;
using ArchLucid.Application.Evolution;
using ArchLucid.Contracts.Evolution;
using ArchLucid.Contracts.ProductLearning;
using ArchLucid.Persistence.Coordination.Evolution;
using ArchLucid.Persistence.Coordination.ProductLearning.Planning;

using FluentAssertions;

using Moq;

namespace ArchLucid.Api.Tests.Services.Evolution;

/// <summary>
///     <see cref="EvolutionSimulationService.RunShadowEvaluationAsync" /> when stored <c>PlanSnapshotJson</c> does not
///     deserialize.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Suite", "Core")]
public sealed class EvolutionSimulationServiceInvalidPlanSnapshotJsonTests
{
    [Fact]
    public async Task RunShadowEvaluationAsync_throws_when_plan_snapshot_json_is_invalid()
    {
        Guid candidateId = Guid.NewGuid();
        ProductLearningScope scope = new()
        {
            TenantId = Guid.NewGuid(), WorkspaceId = Guid.NewGuid(), ProjectId = Guid.NewGuid()
        };

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
            PlanSnapshotJson = "null",
            DerivationRuleVersion = "60R-v1",
            CreatedUtc = DateTime.UtcNow
        };

        Mock<IProductLearningPlanningRepository> planning = new();
        Mock<IEvolutionCandidateChangeSetRepository> candidates = new();
        candidates
            .Setup(c => c.GetByIdAsync(candidateId, scope, It.IsAny<CancellationToken>()))
            .ReturnsAsync(candidate);

        Mock<IEvolutionSimulationRunRepository> runs = new();
        Mock<IArchitectureAnalysisService> analysis = new();
        Mock<ISimulationEvaluationService> evaluation = new();

        EvolutionSimulationService sut = new(
            planning.Object,
            candidates.Object,
            runs.Object,
            analysis.Object,
            evaluation.Object);

        Func<Task> act = () => sut.RunShadowEvaluationAsync(candidateId, scope, CancellationToken.None);

        (await act.Should().ThrowAsync<InvalidOperationException>())
            .Which.Message.Should().Contain("Stored plan snapshot");

        runs.Verify(r => r.InsertAsync(It.IsAny<EvolutionSimulationRunRecord>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
