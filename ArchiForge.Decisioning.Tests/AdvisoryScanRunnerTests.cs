using ArchiForge.Core.Audit;
using ArchiForge.Core.Scoping;
using ArchiForge.Decisioning.Advisory.Delivery;
using ArchiForge.Decisioning.Advisory.Learning;
using ArchiForge.Decisioning.Advisory.Scheduling;
using ArchiForge.Decisioning.Advisory.Services;
using ArchiForge.Decisioning.Advisory.Workflow;
using ArchiForge.Decisioning.Alerts;
using ArchiForge.Decisioning.Alerts.Composite;
using ArchiForge.Decisioning.Comparison;
using ArchiForge.Decisioning.Governance.PolicyPacks;
using ArchiForge.Persistence.Advisory;
using ArchiForge.Persistence.Queries;

using Moq;

namespace ArchiForge.Decisioning.Tests;

[Trait("Category", "Unit")]
public sealed class AdvisoryScanRunnerTests
{
    [Fact]
    public async Task RunScheduleAsync_WhenNoRuns_CompletesExecutionAndAdvancesSchedule()
    {
        Mock<IAuthorityQueryService> authority = new();
        authority
            .Setup(x => x.ListRunsByProjectAsync(It.IsAny<ScopeContext>(), It.IsAny<string>(), 2, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        Mock<IAdvisoryScanExecutionRepository> executions = new();
        executions
            .Setup(x => x.CreateAsync(It.IsAny<AdvisoryScanExecution>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        executions
            .Setup(x => x.UpdateAsync(It.IsAny<AdvisoryScanExecution>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        Mock<IAdvisoryScanScheduleRepository> schedules = new();
        schedules
            .Setup(x => x.UpdateAsync(It.IsAny<AdvisoryScanSchedule>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        Mock<IScanScheduleCalculator> calculator = new();
        calculator
            .Setup(x => x.ComputeNextRunUtc(It.IsAny<string>(), It.IsAny<DateTime>()))
            .Returns(DateTime.UtcNow.AddDays(1));

        Mock<IAuditService> audit = new();
        audit
            .Setup(x => x.LogAsync(It.IsAny<AuditEvent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        AdvisoryScanRunner sut = new(
            authority.Object,
            Mock.Of<IImprovementAdvisorService>(),
            Mock.Of<IComparisonService>(),
            Mock.Of<IArchitectureDigestBuilder>(),
            Mock.Of<IArchitectureDigestRepository>(),
            Mock.Of<IDigestDeliveryDispatcher>(),
            Mock.Of<IAlertService>(),
            Mock.Of<ICompositeAlertService>(),
            Mock.Of<IEffectiveGovernanceLoader>(),
            Mock.Of<IRecommendationRepository>(),
            Mock.Of<IRecommendationLearningService>(),
            executions.Object,
            schedules.Object,
            calculator.Object,
            audit.Object);

        AdvisoryScanSchedule schedule = new()
        {
            TenantId = Guid.NewGuid(),
            WorkspaceId = Guid.NewGuid(),
            ProjectId = Guid.NewGuid(),
            RunProjectSlug = "default"
        };

        await sut.RunScheduleAsync(schedule, CancellationToken.None);

        executions.Verify(
            x => x.UpdateAsync(It.Is<AdvisoryScanExecution>(e => e.Status == "Completed"), It.IsAny<CancellationToken>()),
            Times.Once);
        schedules.Verify(x => x.UpdateAsync(It.IsAny<AdvisoryScanSchedule>(), It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }
}
