using ArchLucid.Core.Audit;
using ArchLucid.Core.Integration;
using ArchLucid.Core.Scoping;
using ArchLucid.Decisioning.Advisory.Delivery;
using ArchLucid.Decisioning.Advisory.Learning;
using ArchLucid.Decisioning.Advisory.Models;
using ArchLucid.Decisioning.Advisory.Scheduling;
using ArchLucid.Decisioning.Advisory.Services;
using ArchLucid.Decisioning.Advisory.Workflow;
using ArchLucid.Decisioning.Alerts;
using ArchLucid.Decisioning.Alerts.Composite;
using ArchLucid.Decisioning.Comparison;
using ArchLucid.Decisioning.Governance.PolicyPacks;
using ArchLucid.Decisioning.Models;
using ArchLucid.Persistence.Advisory;
using ArchLucid.Persistence.Integration;
using ArchLucid.Persistence.Models;
using ArchLucid.Persistence.Queries;

using System.Text.Json;

using FluentAssertions;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using Moq;

namespace ArchLucid.Decisioning.Tests;

/// <summary>
/// Tests for Advisory Scan Runner.
/// </summary>
[Trait("Suite", "Core")]
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
            audit.Object,
            Mock.Of<IIntegrationEventPublisher>(),
            Mock.Of<IIntegrationEventOutboxRepository>(),
            OptionsMonitor(),
            NullLogger<AdvisoryScanRunner>.Instance);

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

    [Fact]
    public async Task RunScheduleAsync_WhenLatestRunHasGoldenManifest_PersistsDigestAndDelivers()
    {
        Guid tenantId = Guid.NewGuid();
        Guid workspaceId = Guid.NewGuid();
        Guid projectId = Guid.NewGuid();
        Guid runId = Guid.NewGuid();
        Guid digestId = Guid.NewGuid();

        GoldenManifest manifest = new()
        {
            TenantId = tenantId,
            WorkspaceId = workspaceId,
            ProjectId = projectId,
            ManifestId = Guid.NewGuid(),
            RunId = runId,
            ContextSnapshotId = Guid.NewGuid(),
            GraphSnapshotId = Guid.NewGuid(),
            FindingsSnapshotId = Guid.NewGuid(),
            DecisionTraceId = Guid.NewGuid(),
            CreatedUtc = DateTime.UtcNow,
            ManifestHash = "h",
            RuleSetId = "rs",
            RuleSetVersion = "1",
            RuleSetHash = "rh",
        };

        RunRecord runRecord = new()
        {
            TenantId = tenantId,
            WorkspaceId = workspaceId,
            ScopeProjectId = projectId,
            RunId = runId,
            ProjectId = "default",
            CreatedUtc = DateTime.UtcNow,
            GoldenManifestId = manifest.ManifestId,
        };

        Mock<IAuthorityQueryService> authority = new();
        authority
            .Setup(x => x.ListRunsByProjectAsync(It.IsAny<ScopeContext>(), It.IsAny<string>(), 2, It.IsAny<CancellationToken>()))
            .ReturnsAsync([new RunSummaryDto { RunId = runId, CreatedUtc = DateTime.UtcNow, ProjectId = "default" }]);
        authority
            .Setup(x => x.GetRunDetailAsync(It.IsAny<ScopeContext>(), runId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                new RunDetailDto
                {
                    Run = runRecord,
                    GoldenManifest = manifest,
                    FindingsSnapshot = null,
                });

        Mock<IImprovementAdvisorService> advisor = new();
        advisor
            .Setup(x => x.GeneratePlanAsync(It.IsAny<GoldenManifest>(), It.IsAny<FindingsSnapshot>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ImprovementPlan { RunId = runId, Recommendations = [] });

        Mock<IRecommendationRepository> recommendations = new();
        recommendations
            .Setup(x => x.ListByRunAsync(tenantId, workspaceId, projectId, runId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        Mock<IRecommendationLearningService> learning = new();
        learning
            .Setup(x => x.GetLatestProfileAsync(tenantId, workspaceId, projectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((RecommendationLearningProfile?)null);

        Mock<IEffectiveGovernanceLoader> governance = new();
        governance
            .Setup(x => x.LoadEffectiveContentAsync(tenantId, workspaceId, projectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PolicyPackContentDocument());

        Mock<IAlertService> simpleAlerts = new();
        simpleAlerts
            .Setup(x => x.EvaluateAndPersistAsync(It.IsAny<AlertEvaluationContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AlertEvaluationOutcome([], []));

        Mock<ICompositeAlertService> compositeAlerts = new();
        compositeAlerts
            .Setup(x => x.EvaluateAndPersistAsync(It.IsAny<AlertEvaluationContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CompositeAlertEvaluationResult([], 0));

        ArchitectureDigest builtDigest = new()
        {
            DigestId = digestId,
            TenantId = tenantId,
            WorkspaceId = workspaceId,
            ProjectId = projectId,
            RunId = runId,
            Title = "t",
            Summary = "s",
            ContentMarkdown = "m",
        };

        Mock<IArchitectureDigestBuilder> digestBuilder = new();
        digestBuilder
            .Setup(
                x => x.Build(
                    tenantId,
                    workspaceId,
                    projectId,
                    runId,
                    null,
                    It.IsAny<ImprovementPlan>(),
                    It.IsAny<IReadOnlyList<AlertRecord>>()))
            .Returns(builtDigest);

        Mock<IArchitectureDigestRepository> digestRepo = new();
        digestRepo.Setup(x => x.CreateAsync(It.IsAny<ArchitectureDigest>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        Mock<IDigestDeliveryDispatcher> delivery = new();
        delivery.Setup(x => x.DeliverAsync(It.IsAny<ArchitectureDigest>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        string? lastResultJson = null;
        Mock<IAdvisoryScanExecutionRepository> executions = new();
        executions
            .Setup(x => x.CreateAsync(It.IsAny<AdvisoryScanExecution>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        executions
            .Setup(x => x.UpdateAsync(It.IsAny<AdvisoryScanExecution>(), It.IsAny<CancellationToken>()))
            .Callback<AdvisoryScanExecution, CancellationToken>((e, _) => lastResultJson = e.ResultJson)
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
            advisor.Object,
            Mock.Of<IComparisonService>(),
            digestBuilder.Object,
            digestRepo.Object,
            delivery.Object,
            simpleAlerts.Object,
            compositeAlerts.Object,
            governance.Object,
            recommendations.Object,
            learning.Object,
            executions.Object,
            schedules.Object,
            calculator.Object,
            audit.Object,
            Mock.Of<IIntegrationEventPublisher>(),
            Mock.Of<IIntegrationEventOutboxRepository>(),
            OptionsMonitor(),
            NullLogger<AdvisoryScanRunner>.Instance);

        AdvisoryScanSchedule schedule = new()
        {
            TenantId = tenantId,
            WorkspaceId = workspaceId,
            ProjectId = projectId,
            RunProjectSlug = "default",
        };

        await sut.RunScheduleAsync(schedule, CancellationToken.None);

        digestRepo.Verify(x => x.CreateAsync(It.Is<ArchitectureDigest>(d => d.DigestId == digestId), It.IsAny<CancellationToken>()), Times.Once);
        delivery.Verify(x => x.DeliverAsync(It.Is<ArchitectureDigest>(d => d.DigestId == digestId), It.IsAny<CancellationToken>()), Times.Once);

        lastResultJson.Should().NotBeNull();
        using (JsonDocument doc = JsonDocument.Parse(lastResultJson!))
        {
            JsonElement root = doc.RootElement;
            root.GetProperty("schemaVersion").GetInt32().Should().Be(1);
            JsonElement tc = root.GetProperty("traceCompleteness");
            tc.GetProperty("totalFindings").GetInt32().Should().Be(0);
            tc.GetProperty("overallCompletenessRatio").GetDouble().Should().Be(0.0);
            tc.GetProperty("byEngine").GetArrayLength().Should().Be(0);
        }
    }

    private static IOptionsMonitor<IntegrationEventsOptions> OptionsMonitor(bool transactionalOutbox = false)
    {
        Mock<IOptionsMonitor<IntegrationEventsOptions>> mock = new();
        mock.Setup(m => m.CurrentValue)
            .Returns(new IntegrationEventsOptions { TransactionalOutboxEnabled = transactionalOutbox });

        return mock.Object;
    }
}
