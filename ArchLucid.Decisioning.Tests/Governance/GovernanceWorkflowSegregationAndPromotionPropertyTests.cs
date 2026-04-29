using System.Data;

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
/// Additional FsCheck coverage for segregation-of-duties and prod promotion guards on <see cref="GovernanceWorkflowService"/>.
/// </summary>
[Trait("Suite", "Core")]
public sealed class GovernanceWorkflowSegregationAndPromotionPropertyTests
{
#pragma warning disable xUnit1031

    [Property(MaxTest = 30)]
    public void ApproveAsync_throws_GovernanceSelfApprovalException_when_reviewer_equals_requested_by(
        NonEmptyString actorGen)
    {
        string actor = actorGen.Get.Trim();

        if (actor.Length == 0)
        {
            return;
        }

        GovernanceWorkflowService sut = CreateSutWithSubmittedRequest(requestedBy: actor);

        Action act = () => sut.ApproveAsync("ar1", actor, actor, null, CancellationToken.None).GetAwaiter().GetResult();

        act.Should().Throw<GovernanceSelfApprovalException>()
            .Which.ApprovalRequestId.Should().Be("ar1");
    }

    [Property(MaxTest = 30)]
    public void RejectAsync_throws_GovernanceSelfApprovalException_when_reviewer_equals_requested_by(
        NonEmptyString actorGen)
    {
        string actor = actorGen.Get.Trim();

        if (actor.Length == 0)
        {
            return;
        }

        GovernanceWorkflowService sut = CreateSutWithSubmittedRequest(requestedBy: actor);

        Action act = () => sut.RejectAsync("ar1", actor, actor, null, CancellationToken.None).GetAwaiter().GetResult();

        act.Should().Throw<GovernanceSelfApprovalException>();
    }

    [Property(MaxTest = 40)]
    public void ApproveAsync_does_not_throw_self_approval_when_reviewer_differs_case_insensitively_from_submitter(
        NonEmptyString submitterGen,
        NonEmptyString reviewerGen)
    {
        string submitter = submitterGen.Get.Trim();
        string reviewer = reviewerGen.Get.Trim();

        if (submitter.Length == 0 || reviewer.Length == 0)
        {
            return;
        }

        if (string.Equals(submitter, reviewer, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        GovernanceWorkflowService sut = CreateSutWithSubmittedRequest(requestedBy: submitter);

        Action act = () => sut.ApproveAsync("ar1", reviewer, reviewer, null, CancellationToken.None).GetAwaiter().GetResult();

        act.Should().NotThrow();
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void ApproveAsync_throws_when_entra_jwt_actor_key_matches_but_display_names_differ()
    {
        const string sharedKey = "jwt:aaa11111-aaaa-aaaa-aaaa-aaaaaaaaaaaa:bbb22222-bbbb-bbbb-bbbb-bbbbbbbbbbbb";

        GovernanceWorkflowService sut = CreateSutWithSubmittedRequest(
            requestedBy: "alice@contoso.com",
            requestedByActorKey: sharedKey);

        Action act = () =>
            sut.ApproveAsync("ar1", "ci-sp-display-name", sharedKey, null, CancellationToken.None).GetAwaiter()
                .GetResult();

        act.Should().Throw<GovernanceSelfApprovalException>();
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task PromoteAsync_to_prod_without_approval_request_id_throws_InvalidOperationException()
    {
        GovernanceWorkflowService sut = CreateSutForPromote(runId: "run1");

        Func<Task> act = () => sut.PromoteAsync(
            "run1",
            "v1",
            GovernanceEnvironment.Test,
            GovernanceEnvironment.Prod,
            "promoter",
            approvalRequestId: null,
            notes: null,
            dryRun: false,
            CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*approval request*");
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task PromoteAsync_to_prod_when_approval_not_approved_throws_InvalidOperationException()
    {
        Mock<IGovernanceApprovalRequestRepository> approvalRepo = new();
        approvalRepo
            .Setup(r => r.GetByIdAsync("ar-pending", It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                new GovernanceApprovalRequest
                {
                    ApprovalRequestId = "ar-pending",
                    Status = GovernanceApprovalStatus.Submitted,
                    RunId = "run1",
                    ManifestVersion = "v1",
                    TargetEnvironment = GovernanceEnvironment.Prod,
                    RequestedBy = "alice",
                });

        GovernanceWorkflowService sut = CreateSutForPromote(runId: "run1", approvalRepo);

        Func<Task> act = () => sut.PromoteAsync(
            "run1",
            "v1",
            GovernanceEnvironment.Test,
            GovernanceEnvironment.Prod,
            "promoter",
            approvalRequestId: "ar-pending",
            notes: null,
            dryRun: false,
            CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*approved*");
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task PromoteAsync_to_prod_when_approval_manifest_version_mismatch_throws_InvalidOperationException()
    {
        Mock<IGovernanceApprovalRequestRepository> approvalRepo = new();
        approvalRepo
            .Setup(r => r.GetByIdAsync("ar1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                new GovernanceApprovalRequest
                {
                    ApprovalRequestId = "ar1",
                    Status = GovernanceApprovalStatus.Approved,
                    RunId = "run1",
                    ManifestVersion = "v-other",
                    TargetEnvironment = GovernanceEnvironment.Prod,
                    RequestedBy = "alice",
                });

        GovernanceWorkflowService sut = CreateSutForPromote(runId: "run1", approvalRepo);

        Func<Task> act = () => sut.PromoteAsync(
            "run1",
            "v1",
            GovernanceEnvironment.Test,
            GovernanceEnvironment.Prod,
            "promoter",
            approvalRequestId: "ar1",
            notes: null,
            dryRun: false,
            CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*manifest version*");
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task PromoteAsync_to_prod_when_approval_run_mismatch_throws_InvalidOperationException()
    {
        Mock<IGovernanceApprovalRequestRepository> approvalRepo = new();
        approvalRepo
            .Setup(r => r.GetByIdAsync("ar1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                new GovernanceApprovalRequest
                {
                    ApprovalRequestId = "ar1",
                    Status = GovernanceApprovalStatus.Approved,
                    RunId = "other-run",
                    ManifestVersion = "v1",
                    TargetEnvironment = GovernanceEnvironment.Prod,
                    RequestedBy = "alice",
                });

        GovernanceWorkflowService sut = CreateSutForPromote(runId: "run1", approvalRepo);

        Func<Task> act = () => sut.PromoteAsync(
            "run1",
            "v1",
            GovernanceEnvironment.Test,
            GovernanceEnvironment.Prod,
            "promoter",
            approvalRequestId: "ar1",
            notes: null,
            dryRun: false,
            CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not 'run1'*");
    }

    private static GovernanceWorkflowService CreateSutWithSubmittedRequest(
        string requestedBy,
        string? requestedByActorKey = null)
    {
        Mock<IGovernanceApprovalRequestRepository> approvalRepo = new();
        GovernanceApprovalRequest request = new()
        {
            ApprovalRequestId = "ar1",
            Status = GovernanceApprovalStatus.Submitted,
            RunId = "run1",
            RequestedBy = requestedBy,
            RequestedByActorKey = requestedByActorKey,
        };

        approvalRepo.Setup(r => r.GetByIdAsync("ar1", It.IsAny<CancellationToken>())).ReturnsAsync(request);
        approvalRepo
            .Setup(
                r => r.TryTransitionFromReviewableAsync(
                    "ar1",
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string?>(),
                    It.IsAny<string?>(),
                    It.IsAny<DateTime>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        return GovernanceWorkflowTestFactory.CreateWithApprovalRepo(approvalRepo);
    }

    private static GovernanceWorkflowService CreateSutForPromote(
        string runId,
        Mock<IGovernanceApprovalRequestRepository>? approvalRepo = null)
    {
        approvalRepo ??= new Mock<IGovernanceApprovalRequestRepository>();

        Mock<IGovernancePromotionRecordRepository> promotionRepo = new();
        Mock<IGovernanceEnvironmentActivationRepository> activationRepo = new();
        Mock<IRunDetailQueryService> runDetail = new();

        runDetail
            .Setup(s => s.GetRunDetailAsync(runId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                new ArchitectureRunDetail
                {
                    Run = new ArchitectureRun { RunId = runId, RequestId = "req1" },
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

#pragma warning restore xUnit1031
}
