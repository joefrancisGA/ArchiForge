using ArchLucid.Application;
using ArchLucid.Application.Common;
using ArchLucid.Application.Governance;
using ArchLucid.TestSupport;
using ArchLucid.Contracts.Architecture;
using ArchLucid.Contracts.Governance;
using ArchLucid.Contracts.Metadata;
using ArchLucid.Core.Audit;
using ArchLucid.Core.Integration;
using ArchLucid.Core.Scoping;
using ArchLucid.Persistence.Data.Repositories;
using ArchLucid.Persistence.Integration;

using FluentAssertions;

using FsCheck;

using FsCheck.Xunit;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Moq;

using System.Data;

namespace ArchLucid.Decisioning.Tests.Governance;

/// <summary>
/// Property-based checks for <see cref="GovernanceWorkflowService"/> approval lifecycle and activation side effects.
/// </summary>
[Trait("Suite", "Core")]
public sealed class GovernanceWorkflowPropertyTests
{
#pragma warning disable xUnit1031 // FsCheck properties are synchronous; workflow methods are async.

    [Property(Arbitrary = new[] { typeof(GovernanceWorkflowArbitraries) }, MaxTest = 30)]
    public void ApproveOnlyLegalFromDraftOrSubmitted(GovernanceStatusSample sample)
    {
        GovernanceWorkflowService sut = GovernanceWorkflowTestFactory.CreateForApprove(sample.Status);

        Action act = () => sut.ApproveAsync("ar1", "reviewer", null, CancellationToken.None).GetAwaiter().GetResult();

        bool legal = sample.Status == GovernanceApprovalStatus.Draft || sample.Status == GovernanceApprovalStatus.Submitted;

        if (legal)
        {
            act.Should().NotThrow();
        }
        else
        {
            act.Should().Throw<InvalidOperationException>();
        }
    }

    [Property(Arbitrary = new[] { typeof(GovernanceWorkflowArbitraries) }, MaxTest = 30)]
    public void RejectOnlyLegalFromDraftOrSubmitted(GovernanceStatusSample sample)
    {
        GovernanceWorkflowService sut = GovernanceWorkflowTestFactory.CreateForApprove(sample.Status);

        Action act = () => sut.RejectAsync("ar1", "reviewer", null, CancellationToken.None).GetAwaiter().GetResult();

        bool legal = sample.Status == GovernanceApprovalStatus.Draft || sample.Status == GovernanceApprovalStatus.Submitted;

        if (legal)
        {
            act.Should().NotThrow();
        }
        else
        {
            act.Should().Throw<InvalidOperationException>();
        }
    }

    [Property(Arbitrary = new[] { typeof(GovernanceWorkflowArbitraries) }, MaxTest = 30)]
    public void StatusAfterApproveIsAlwaysApproved(DraftOrSubmittedSample sample)
    {
        Mock<IGovernanceApprovalRequestRepository> approvalRepo = new();
        GovernanceApprovalRequest request = new()
        {
            ApprovalRequestId = "ar1",
            Status = sample.Status,
            RunId = "run1",
        };

        approvalRepo.Setup(r => r.GetByIdAsync("ar1", It.IsAny<CancellationToken>())).ReturnsAsync(request);

        GovernanceApprovalRequest? updated = null;
        approvalRepo
            .Setup(r => r.UpdateAsync(It.IsAny<GovernanceApprovalRequest>(), It.IsAny<CancellationToken>()))
            .Callback<GovernanceApprovalRequest, CancellationToken>((r, _) => updated = r)
            .Returns(Task.CompletedTask);

        GovernanceWorkflowService sut = GovernanceWorkflowTestFactory.CreateWithApprovalRepo(approvalRepo);

        sut.ApproveAsync("ar1", "reviewer", null, CancellationToken.None).GetAwaiter().GetResult();

        updated.Should().NotBeNull();
        updated!.Status.Should().Be(GovernanceApprovalStatus.Approved);
    }

    [Property(Arbitrary = new[] { typeof(GovernanceWorkflowArbitraries) }, MaxTest = 30)]
    public void StatusAfterRejectIsAlwaysRejected(DraftOrSubmittedSample sample)
    {
        Mock<IGovernanceApprovalRequestRepository> approvalRepo = new();
        GovernanceApprovalRequest request = new()
        {
            ApprovalRequestId = "ar1",
            Status = sample.Status,
            RunId = "run1",
        };

        approvalRepo.Setup(r => r.GetByIdAsync("ar1", It.IsAny<CancellationToken>())).ReturnsAsync(request);

        GovernanceApprovalRequest? updated = null;
        approvalRepo
            .Setup(r => r.UpdateAsync(It.IsAny<GovernanceApprovalRequest>(), It.IsAny<CancellationToken>()))
            .Callback<GovernanceApprovalRequest, CancellationToken>((r, _) => updated = r)
            .Returns(Task.CompletedTask);

        GovernanceWorkflowService sut = GovernanceWorkflowTestFactory.CreateWithApprovalRepo(approvalRepo);

        sut.RejectAsync("ar1", "reviewer", null, CancellationToken.None).GetAwaiter().GetResult();

        updated.Should().NotBeNull();
        updated!.Status.Should().Be(GovernanceApprovalStatus.Rejected);
    }

    [Property(Arbitrary = new[] { typeof(GovernanceWorkflowArbitraries) }, MaxTest = 25)]
    public void ActivateDeactivatesAllExistingActiveRecords(ActiveActivationCount count)
    {
        Mock<IGovernanceEnvironmentActivationRepository> activationRepo = new();
        Mock<IRunDetailQueryService> runDetail = new();

        runDetail
            .Setup(s => s.GetRunDetailAsync("run1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                new ArchitectureRunDetail
                {
                    Run = new ArchitectureRun { RunId = "run1", RequestId = "req1" },
                });

        List<GovernanceEnvironmentActivation> existing = [];

        for (int i = 0; i < count.Value; i++)
        {
            existing.Add(
                new GovernanceEnvironmentActivation
                {
                    ActivationId = $"act{i}",
                    RunId = "prior-run",
                    ManifestVersion = "v1",
                    Environment = GovernanceEnvironment.Dev,
                    IsActive = true,
                });
        }

        activationRepo
            .Setup(r => r.GetByEnvironmentAsync(GovernanceEnvironment.Dev, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        activationRepo
            .Setup(r => r.UpdateAsync(It.IsAny<GovernanceEnvironmentActivation>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        activationRepo
            .Setup(r => r.CreateAsync(It.IsAny<GovernanceEnvironmentActivation>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        GovernanceWorkflowService sut = GovernanceWorkflowTestFactory.CreateWithActivationRepo(activationRepo, runDetail);

        sut.ActivateAsync("run1", "v2", GovernanceEnvironment.Dev, "activator", CancellationToken.None).GetAwaiter().GetResult();

        activationRepo.Verify(
            r => r.UpdateAsync(It.Is<GovernanceEnvironmentActivation>(a => a.IsActive == false), It.IsAny<CancellationToken>()),
            Times.Exactly(count.Value));

        activationRepo.Verify(
            r => r.CreateAsync(It.Is<GovernanceEnvironmentActivation>(a => a.IsActive), It.IsAny<CancellationToken>()),
            Times.Once());
    }

#pragma warning restore xUnit1031
}

/// <summary>Wrapped governance approval status for FsCheck (avoids clashing with other string arbitraries).</summary>
public readonly record struct GovernanceStatusSample(string Status);

/// <summary>Only <see cref="GovernanceApprovalStatus.Draft"/> or <see cref="GovernanceApprovalStatus.Submitted"/>.</summary>
public readonly record struct DraftOrSubmittedSample(string Status);

/// <summary>Count of pre-existing active activations (0–5).</summary>
public readonly record struct ActiveActivationCount(int Value);

/// <summary>FsCheck registration for <see cref="GovernanceWorkflowPropertyTests"/>.</summary>
public static class GovernanceWorkflowArbitraries
{
    public static Arbitrary<GovernanceStatusSample> GovernanceStatuses()
    {
        Gen<string> gen = Gen.Elements(
            GovernanceApprovalStatus.Draft,
            GovernanceApprovalStatus.Submitted,
            GovernanceApprovalStatus.Approved,
            GovernanceApprovalStatus.Rejected,
            GovernanceApprovalStatus.Promoted,
            GovernanceApprovalStatus.Activated);

        return gen.Select(s => new GovernanceStatusSample(s)).ToArbitrary();
    }

    public static Arbitrary<DraftOrSubmittedSample> DraftOrSubmittedStatuses()
    {
        return Gen.Elements(GovernanceApprovalStatus.Draft, GovernanceApprovalStatus.Submitted)
            .Select(s => new DraftOrSubmittedSample(s))
            .ToArbitrary();
    }

    public static Arbitrary<ActiveActivationCount> ActiveActivationCounts()
    {
        return Gen.Choose(0, 5).Select(v => new ActiveActivationCount(v)).ToArbitrary();
    }
}

internal static class GovernanceWorkflowTestFactory
{
    internal static GovernanceWorkflowService CreateForApprove(string status)
    {
        Mock<IGovernanceApprovalRequestRepository> approvalRepo = new();
        GovernanceApprovalRequest request = new()
        {
            ApprovalRequestId = "ar1",
            Status = status,
            RunId = "run1",
        };

        approvalRepo.Setup(r => r.GetByIdAsync("ar1", It.IsAny<CancellationToken>())).ReturnsAsync(request);
        approvalRepo
            .Setup(r => r.UpdateAsync(It.IsAny<GovernanceApprovalRequest>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        return CreateWithApprovalRepo(approvalRepo);
    }

    internal static GovernanceWorkflowService CreateWithApprovalRepo(Mock<IGovernanceApprovalRequestRepository> approvalRepo)
    {
        Mock<IGovernancePromotionRecordRepository> promotionRepo = new();
        Mock<IGovernanceEnvironmentActivationRepository> activationRepo = new();
        Mock<IRunDetailQueryService> runDetail = new();

        runDetail
            .Setup(s => s.GetRunDetailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                new ArchitectureRunDetail
                {
                    Run = new ArchitectureRun { RunId = "run1", RequestId = "req1" },
                });

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
            .Setup(p => p.PublishAsync(It.IsAny<string>(), It.IsAny<ReadOnlyMemory<byte>>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
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
            ArchLucidUnitOfWorkTestDoubles.InMemoryModeFactory(),
            logger.Object);
    }

    internal static GovernanceWorkflowService CreateWithActivationRepo(
        Mock<IGovernanceEnvironmentActivationRepository> activationRepo,
        Mock<IRunDetailQueryService> runDetail)
    {
        Mock<IGovernanceApprovalRequestRepository> approvalRepo = new();
        Mock<IGovernancePromotionRecordRepository> promotionRepo = new();

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
            .Setup(p => p.PublishAsync(It.IsAny<string>(), It.IsAny<ReadOnlyMemory<byte>>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
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
                    It.IsAny<IDbConnection>(),
                    It.IsAny<IDbTransaction>(),
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
}
