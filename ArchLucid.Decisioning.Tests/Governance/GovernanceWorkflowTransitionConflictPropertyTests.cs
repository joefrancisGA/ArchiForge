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
/// FsCheck and focused facts for <see cref="GovernanceWorkflowService"/> paths where
/// <see cref="IGovernanceApprovalRequestRepository.TryTransitionFromReviewableAsync"/> loses a race
/// and the refreshed request is already in a terminal review state.
/// </summary>
[Trait("Suite", "Core")]
public sealed class GovernanceWorkflowTransitionConflictPropertyTests
{
#pragma warning disable xUnit1031

    [Property(MaxTest = 25)]
    public void ApproveAsync_when_transition_loses_race_and_peer_already_approved_throws_conflict(
        NonEmptyString idGen)
    {
        string approvalRequestId = idGen.Get.Trim();

        if (approvalRequestId.Length == 0)
        {
            return;
        }

        Mock<IGovernanceApprovalRequestRepository> approvalRepo = new();
        GovernanceApprovalRequest initial = new()
        {
            ApprovalRequestId = approvalRequestId,
            Status = GovernanceApprovalStatus.Submitted,
            RunId = "run1",
            RequestedBy = "alice",
        };

        GovernanceApprovalRequest fresh = new()
        {
            ApprovalRequestId = approvalRequestId,
            Status = GovernanceApprovalStatus.Approved,
            RunId = "run1",
            RequestedBy = "alice",
        };

        approvalRepo
            .SetupSequence(r => r.GetByIdAsync(approvalRequestId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(initial)
            .ReturnsAsync(fresh);

        approvalRepo
            .Setup(
                r => r.TryTransitionFromReviewableAsync(
                    approvalRequestId,
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string?>(),
                    It.IsAny<string?>(),
                    It.IsAny<DateTime>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        GovernanceWorkflowService sut = GovernanceWorkflowTestFactory.CreateWithApprovalRepo(approvalRepo);

        Action act = () =>
            sut.ApproveAsync(approvalRequestId, "bob", "bob", null, CancellationToken.None).GetAwaiter().GetResult();

        act.Should().Throw<GovernanceApprovalReviewConflictException>()
            .Which.ApprovalRequestId.Should().Be(approvalRequestId);
    }

    [Property(MaxTest = 25)]
    public void RejectAsync_when_transition_loses_race_and_peer_already_rejected_throws_conflict(
        NonEmptyString idGen)
    {
        string approvalRequestId = idGen.Get.Trim();

        if (approvalRequestId.Length == 0)
        {
            return;
        }

        Mock<IGovernanceApprovalRequestRepository> approvalRepo = new();
        GovernanceApprovalRequest initial = new()
        {
            ApprovalRequestId = approvalRequestId,
            Status = GovernanceApprovalStatus.Submitted,
            RunId = "run1",
            RequestedBy = "alice",
        };

        GovernanceApprovalRequest fresh = new()
        {
            ApprovalRequestId = approvalRequestId,
            Status = GovernanceApprovalStatus.Rejected,
            RunId = "run1",
            RequestedBy = "alice",
        };

        approvalRepo
            .SetupSequence(r => r.GetByIdAsync(approvalRequestId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(initial)
            .ReturnsAsync(fresh);

        approvalRepo
            .Setup(
                r => r.TryTransitionFromReviewableAsync(
                    approvalRequestId,
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string?>(),
                    It.IsAny<string?>(),
                    It.IsAny<DateTime>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        GovernanceWorkflowService sut = GovernanceWorkflowTestFactory.CreateWithApprovalRepo(approvalRepo);

        Action act = () =>
            sut.RejectAsync(approvalRequestId, "bob", "bob", null, CancellationToken.None).GetAwaiter().GetResult();

        act.Should().Throw<GovernanceApprovalReviewConflictException>()
            .Which.ApprovalRequestId.Should().Be(approvalRequestId);
    }

    [Property(Arbitrary = [typeof(InvalidGovernancePromotionArb)], MaxTest = 40)]
    public void SubmitApprovalRequestAsync_throws_when_environment_order_invalid(InvalidPromotionPair pair)
    {
        Mock<IRunDetailQueryService> runDetail = new();
        runDetail
            .Setup(s => s.GetRunDetailAsync("run1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                new ArchitectureRunDetail
                {
                    Run = new ArchitectureRun { RunId = "run1", RequestId = "req1" },
                });

        GovernanceWorkflowService sut = CreateSubmitSut(runDetail);

        Action act = () =>
            sut.SubmitApprovalRequestAsync(
                    "run1",
                    "v1",
                    pair.Source,
                    pair.Target,
                    "requester",
                    null,
                    null,
                    dryRun: false,
                    CancellationToken.None)
                .GetAwaiter()
                .GetResult();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*environment ordering*");
    }

    private static GovernanceWorkflowService CreateSubmitSut(Mock<IRunDetailQueryService> runDetail)
    {
        Mock<IGovernanceApprovalRequestRepository> approvalRepo = new();
        Mock<IGovernancePromotionRecordRepository> promotionRepo = new();
        Mock<IGovernanceEnvironmentActivationRepository> activationRepo = new();

        Mock<IBaselineMutationAuditService> audit = new();
        audit
            .Setup(
                a => a.RecordAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string?>(),
                    It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        Mock<IAuditService> durableAudit = new();
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
            audit.Object,
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

/// <summary>Pair of governance environments that is not a valid single-step promotion.</summary>
public sealed record InvalidPromotionPair(string Source, string Target);

/// <summary>FsCheck arbitraries for invalid promotion pairs.</summary>
public static class InvalidGovernancePromotionArb
{
    public static Arbitrary<InvalidPromotionPair> InvalidPairs()
    {
        string[] envs = [GovernanceEnvironment.Dev, GovernanceEnvironment.Test, GovernanceEnvironment.Prod];

        return Gen.Two(Gen.Elements(envs))
            .Where(pair => !GovernanceEnvironmentOrder.IsValidPromotion(pair.Item1, pair.Item2))
            .Select(pair => new InvalidPromotionPair(pair.Item1, pair.Item2))
            .ToArbitrary();
    }
}
