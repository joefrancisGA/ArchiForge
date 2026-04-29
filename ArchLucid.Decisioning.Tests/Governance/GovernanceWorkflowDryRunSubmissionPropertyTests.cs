using ArchLucid.Application;
using ArchLucid.Application.Common;
using ArchLucid.Application.Governance;
using ArchLucid.Contracts.Architecture;
using ArchLucid.Contracts.Governance;
using ArchLucid.Contracts.Metadata;
using ArchLucid.Core.Audit;
using ArchLucid.Core.Integration;
using ArchLucid.Core.Scoping;
using ArchLucid.Persistence;
using ArchLucid.Persistence.Data.Repositories;
using ArchLucid.TestSupport;

using FluentAssertions;

using FsCheck;
using FsCheck.Xunit;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Moq;

namespace ArchLucid.Decisioning.Tests.Governance;

/// <summary>
/// FsCheck coverage for <see cref="GovernanceWorkflowService.SubmitApprovalRequestAsync"/> dry-run path:
/// submitted shape without persistence or side-effect channels.
/// </summary>
[Trait("Suite", "Core")]
[Trait("Category", "Unit")]
public sealed class GovernanceWorkflowDryRunSubmissionPropertyTests
{
#pragma warning disable xUnit1031

    [Property(Arbitrary = [typeof(ValidSingleStepGovernancePromotionArb)], MaxTest = 20)]
    public void SubmitApprovalRequestAsync_dry_run_returns_submitted_shape_and_does_not_persist(
        ValidSingleStepPromotion pair)
    {
        Mock<IGovernanceApprovalRequestRepository> approvalRepo = new();
        Mock<IRunDetailQueryService> runDetail = new();
        runDetail
            .Setup(s => s.GetRunDetailAsync("run1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                new ArchitectureRunDetail
                {
                    Run = new ArchitectureRun { RunId = "run1", RequestId = "req1" },
                });

        Mock<IBaselineMutationAuditService> baseline = new();
        Mock<IAuditService> durableAudit = new();

        GovernanceWorkflowService sut = CreateSubmitSut(runDetail, approvalRepo, baseline, durableAudit);

        GovernanceApprovalRequest result = sut.SubmitApprovalRequestAsync(
                "run1",
                "v9",
                pair.Source,
                pair.Target,
                "requester",
                requestedByActorKey: null,
                requestComment: "note",
                dryRun: true,
                CancellationToken.None)
            .GetAwaiter()
            .GetResult();

        result.Status.Should().Be(GovernanceApprovalStatus.Submitted);
        result.RunId.Should().Be("run1");
        result.ManifestVersion.Should().Be("v9");
        result.SourceEnvironment.Should().Be(pair.Source);
        result.TargetEnvironment.Should().Be(pair.Target);
        result.RequestedBy.Should().Be("requester");
        result.RequestComment.Should().Be("note");

        approvalRepo.Verify(
            r => r.CreateAsync(It.IsAny<GovernanceApprovalRequest>(), It.IsAny<CancellationToken>()),
            Times.Never);

        baseline.Verify(
            a => a.RecordAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()),
            Times.Never);

        durableAudit.Verify(
            a => a.LogAsync(It.IsAny<AuditEvent>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    private static GovernanceWorkflowService CreateSubmitSut(
        Mock<IRunDetailQueryService> runDetail,
        Mock<IGovernanceApprovalRequestRepository> approvalRepo,
        Mock<IBaselineMutationAuditService> baseline,
        Mock<IAuditService> durableAudit)
    {
        Mock<IGovernancePromotionRecordRepository> promotionRepo = new();
        Mock<IGovernanceEnvironmentActivationRepository> activationRepo = new();

        baseline
            .Setup(
                a => a.RecordAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string?>(),
                    It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        durableAudit
            .Setup(a => a.LogAsync(It.IsAny<AuditEvent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        Mock<IScopeContextProvider> scopeProvider = new();
        scopeProvider
            .Setup(p => p.GetCurrentScope())
            .Returns(
                new ScopeContext
                {
                    TenantId = Guid.NewGuid(),
                    WorkspaceId = Guid.NewGuid(),
                    ProjectId = Guid.NewGuid(),
                });

        Mock<IIntegrationEventPublisher> publisher = new();
        publisher
            .Setup(p => p.PublishAsync(It.IsAny<string>(), It.IsAny<ReadOnlyMemory<byte>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        publisher
            .Setup(p => p.PublishAsync(It.IsAny<string>(), It.IsAny<ReadOnlyMemory<byte>>(), It.IsAny<string?>(), It.IsAny<IReadOnlyDictionary<string, object>?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        Mock<ILogger<GovernanceWorkflowService>> logger = new();

        Mock<IIntegrationEventOutboxRepository> outbox = CreateIntegrationOutboxStub();
        Mock<IOptionsMonitor<IntegrationEventsOptions>> opts = CreateIntegrationEventsOptionsMonitor();

        return new GovernanceWorkflowService(
            approvalRepo.Object,
            promotionRepo.Object,
            activationRepo.Object,
            runDetail.Object,
            baseline.Object,
            durableAudit.Object,
            scopeProvider.Object,
            publisher.Object,
            outbox.Object,
            opts.Object,
            Options.Create(new PreCommitGovernanceGateOptions()),
            ArchLucidUnitOfWorkTestDoubles.InMemoryModeFactory(),
            logger.Object);
    }

    private static Mock<IIntegrationEventOutboxRepository> CreateIntegrationOutboxStub()
    {
        Mock<IIntegrationEventOutboxRepository> mock = new();
        mock.Setup(
                o => o.EnqueueAsync(
                    It.IsAny<Guid?>(),
                    It.IsAny<string>(),
                    It.IsAny<string?>(),
                    It.IsAny<ReadOnlyMemory<byte>>(),
                    It.IsAny<Guid>(),
                    It.IsAny<Guid>(),
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        mock.Setup(
                o => o.EnqueueAsync(
                    It.IsAny<Guid?>(),
                    It.IsAny<string>(),
                    It.IsAny<string?>(),
                    It.IsAny<ReadOnlyMemory<byte>>(),
                    It.IsAny<Guid>(),
                    It.IsAny<Guid>(),
                    It.IsAny<Guid>(),
                    It.IsAny<System.Data.IDbConnection>(),
                    It.IsAny<System.Data.IDbTransaction>(),
                    It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        return mock;
    }

    private static Mock<IOptionsMonitor<IntegrationEventsOptions>> CreateIntegrationEventsOptionsMonitor()
    {
        Mock<IOptionsMonitor<IntegrationEventsOptions>> mock = new();
        mock.Setup(m => m.CurrentValue).Returns(new IntegrationEventsOptions { TransactionalOutboxEnabled = false });

        return mock;
    }

#pragma warning restore xUnit1031
}

/// <summary>Single-step promotion allowed by <see cref="GovernanceEnvironmentOrder"/>.</summary>
public sealed record ValidSingleStepPromotion(string Source, string Target);

/// <summary>FsCheck arbitrary: dev→test and test→prod.</summary>
public static class ValidSingleStepGovernancePromotionArb
{
    public static Arbitrary<ValidSingleStepPromotion> ValidPairs()
    {
        return Gen
            .Elements(
                new ValidSingleStepPromotion(GovernanceEnvironment.Dev, GovernanceEnvironment.Test),
                new ValidSingleStepPromotion(GovernanceEnvironment.Test, GovernanceEnvironment.Prod))
            .ToArbitrary();
    }
}
