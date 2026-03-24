using ArchiForge.Application;
using ArchiForge.Application.Governance;
using ArchiForge.Contracts.Common;
using ArchiForge.Contracts.Governance;
using ArchiForge.Contracts.Metadata;
using ArchiForge.Data.Repositories;

using FluentAssertions;

using Microsoft.Extensions.Logging;

using Moq;

namespace ArchiForge.Api.Tests;

[Trait("Category", "Unit")]
public sealed class GovernanceWorkflowServiceTests
{
    private readonly Mock<IGovernanceApprovalRequestRepository> _approvalRepo;
    private readonly Mock<IGovernancePromotionRecordRepository> _promotionRepo;
    private readonly Mock<IGovernanceEnvironmentActivationRepository> _activationRepo;
    private readonly Mock<IArchitectureRunRepository> _runRepo;
    private readonly GovernanceWorkflowService _sut;

    public GovernanceWorkflowServiceTests()
    {
        _approvalRepo = new Mock<IGovernanceApprovalRequestRepository>();
        _promotionRepo = new Mock<IGovernancePromotionRecordRepository>();
        _activationRepo = new Mock<IGovernanceEnvironmentActivationRepository>();
        _runRepo = new Mock<IArchitectureRunRepository>();

        var logger = new Mock<ILogger<GovernanceWorkflowService>>();

        _sut = new GovernanceWorkflowService(
            _approvalRepo.Object,
            _promotionRepo.Object,
            _activationRepo.Object,
            _runRepo.Object,
            logger.Object);
    }

    private static ArchitectureRun ValidRun(string runId = "run-1") => new()
    {
        RunId = runId,
        RequestId = "req-1",
        Status = ArchitectureRunStatus.Committed,
        CreatedUtc = DateTime.UtcNow
    };

    // ── Submit ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task SubmitApprovalRequest_RunExists_CreatesSubmittedRequest()
    {
        _runRepo.Setup(r => r.GetByIdAsync("run-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(ValidRun("run-1"));
        _approvalRepo.Setup(r => r.CreateAsync(It.IsAny<GovernanceApprovalRequest>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await _sut.SubmitApprovalRequestAsync(
            "run-1", "v1", "dev", "test", "alice", null);

        result.Status.Should().Be(GovernanceApprovalStatus.Submitted);
        result.RunId.Should().Be("run-1");
        result.ManifestVersion.Should().Be("v1");
        result.SourceEnvironment.Should().Be("dev");
        result.TargetEnvironment.Should().Be("test");
        result.RequestedBy.Should().Be("alice");

        _approvalRepo.Verify(r => r.CreateAsync(It.IsAny<GovernanceApprovalRequest>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SubmitApprovalRequest_RunNotFound_ThrowsRunNotFoundException()
    {
        _runRepo.Setup(r => r.GetByIdAsync("missing-run", It.IsAny<CancellationToken>()))
            .ReturnsAsync((ArchitectureRun?)null);

        var act = () => _sut.SubmitApprovalRequestAsync(
            "missing-run", "v1", "dev", "test", "alice", null);

        await act.Should().ThrowAsync<RunNotFoundException>()
            .WithMessage("*missing-run*");
    }

    // ── Approve ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task Approve_SubmittedRequest_ChangesStatusToApproved()
    {
        var existing = new GovernanceApprovalRequest
        {
            ApprovalRequestId = "apr-1",
            RunId = "run-1",
            ManifestVersion = "v1",
            Status = GovernanceApprovalStatus.Submitted
        };

        _approvalRepo.Setup(r => r.GetByIdAsync("apr-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);
        _approvalRepo.Setup(r => r.UpdateAsync(It.IsAny<GovernanceApprovalRequest>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await _sut.ApproveAsync("apr-1", "bob", "LGTM");

        result.Status.Should().Be(GovernanceApprovalStatus.Approved);
        result.ReviewedBy.Should().Be("bob");
        result.ReviewComment.Should().Be("LGTM");
        result.ReviewedUtc.Should().NotBeNull();

        _approvalRepo.Verify(r => r.UpdateAsync(
            It.Is<GovernanceApprovalRequest>(x => x.Status == GovernanceApprovalStatus.Approved),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Approve_DraftRequest_ChangesStatusToApproved()
    {
        var existing = new GovernanceApprovalRequest
        {
            ApprovalRequestId = "apr-draft",
            Status = GovernanceApprovalStatus.Draft
        };

        _approvalRepo.Setup(r => r.GetByIdAsync("apr-draft", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);
        _approvalRepo.Setup(r => r.UpdateAsync(It.IsAny<GovernanceApprovalRequest>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await _sut.ApproveAsync("apr-draft", "bob", null);

        result.Status.Should().Be(GovernanceApprovalStatus.Approved);
    }

    [Fact]
    public async Task Approve_AlreadyRejected_ThrowsInvalidOperationException()
    {
        var existing = new GovernanceApprovalRequest
        {
            ApprovalRequestId = "apr-rejected",
            Status = GovernanceApprovalStatus.Rejected
        };

        _approvalRepo.Setup(r => r.GetByIdAsync("apr-rejected", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        var act = () => _sut.ApproveAsync("apr-rejected", "bob", null);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*cannot be approved*");
    }

    // ── Reject ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task Reject_SubmittedRequest_ChangesStatusToRejected()
    {
        var existing = new GovernanceApprovalRequest
        {
            ApprovalRequestId = "apr-2",
            Status = GovernanceApprovalStatus.Submitted
        };

        _approvalRepo.Setup(r => r.GetByIdAsync("apr-2", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);
        _approvalRepo.Setup(r => r.UpdateAsync(It.IsAny<GovernanceApprovalRequest>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await _sut.RejectAsync("apr-2", "carol", "Needs more detail");

        result.Status.Should().Be(GovernanceApprovalStatus.Rejected);
        result.ReviewedBy.Should().Be("carol");
        result.ReviewComment.Should().Be("Needs more detail");
    }

    [Fact]
    public async Task Reject_AlreadyApproved_ThrowsInvalidOperationException()
    {
        var existing = new GovernanceApprovalRequest
        {
            ApprovalRequestId = "apr-approved",
            Status = GovernanceApprovalStatus.Approved
        };

        _approvalRepo.Setup(r => r.GetByIdAsync("apr-approved", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        var act = () => _sut.RejectAsync("apr-approved", "carol", null);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*cannot be rejected*");
    }

    // ── Promote ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task Promote_ToProd_WithoutApproval_ThrowsInvalidOperationException()
    {
        var act = () => _sut.PromoteAsync(
            "run-1", "v1", "test", GovernanceEnvironment.Prod,
            "alice", null, null);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*approved approval request*");
    }

    [Fact]
    public async Task Promote_ToProd_WithUnapprovedRequest_ThrowsInvalidOperationException()
    {
        var pendingApproval = new GovernanceApprovalRequest
        {
            ApprovalRequestId = "apr-pending",
            Status = GovernanceApprovalStatus.Submitted
        };

        _approvalRepo.Setup(r => r.GetByIdAsync("apr-pending", It.IsAny<CancellationToken>()))
            .ReturnsAsync(pendingApproval);

        var act = () => _sut.PromoteAsync(
            "run-1", "v1", "test", GovernanceEnvironment.Prod,
            "alice", "apr-pending", null);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*approved approval request*");
    }

    [Fact]
    public async Task Promote_ToProd_WithApprovedRequest_Succeeds()
    {
        var approvedRequest = new GovernanceApprovalRequest
        {
            ApprovalRequestId = "apr-approved",
            Status = GovernanceApprovalStatus.Approved
        };

        _approvalRepo.Setup(r => r.GetByIdAsync("apr-approved", It.IsAny<CancellationToken>()))
            .ReturnsAsync(approvedRequest);
        _approvalRepo.Setup(r => r.UpdateAsync(It.IsAny<GovernanceApprovalRequest>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _promotionRepo.Setup(r => r.CreateAsync(It.IsAny<GovernancePromotionRecord>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await _sut.PromoteAsync(
            "run-1", "v1", "test", GovernanceEnvironment.Prod,
            "alice", "apr-approved", "prod ready");

        result.TargetEnvironment.Should().Be(GovernanceEnvironment.Prod);
        result.PromotedBy.Should().Be("alice");
        result.Notes.Should().Be("prod ready");
        result.ApprovalRequestId.Should().Be("apr-approved");

        _approvalRepo.Verify(r => r.UpdateAsync(
            It.Is<GovernanceApprovalRequest>(x => x.Status == GovernanceApprovalStatus.Promoted),
            It.IsAny<CancellationToken>()), Times.Once);
        _promotionRepo.Verify(r => r.CreateAsync(It.IsAny<GovernancePromotionRecord>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Promote_ToTest_WithoutApproval_Succeeds()
    {
        _promotionRepo.Setup(r => r.CreateAsync(It.IsAny<GovernancePromotionRecord>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await _sut.PromoteAsync(
            "run-1", "v1", "dev", GovernanceEnvironment.Test,
            "alice", null, null);

        result.TargetEnvironment.Should().Be(GovernanceEnvironment.Test);
        _promotionRepo.Verify(r => r.CreateAsync(It.IsAny<GovernancePromotionRecord>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    // ── Activate ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task Activate_DeactivatesPreviousActiveActivation()
    {
        var previous = new GovernanceEnvironmentActivation
        {
            ActivationId = "act-old",
            RunId = "run-1",
            ManifestVersion = "v1",
            Environment = "dev",
            IsActive = true
        };

        _runRepo.Setup(r => r.GetByIdAsync("run-2", It.IsAny<CancellationToken>()))
            .ReturnsAsync(ValidRun("run-2"));
        _activationRepo.Setup(r => r.GetByEnvironmentAsync("dev", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<GovernanceEnvironmentActivation> { previous });
        _activationRepo.Setup(r => r.UpdateAsync(It.IsAny<GovernanceEnvironmentActivation>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _activationRepo.Setup(r => r.CreateAsync(It.IsAny<GovernanceEnvironmentActivation>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await _sut.ActivateAsync("run-2", "v2", "dev");

        result.IsActive.Should().BeTrue();
        result.ManifestVersion.Should().Be("v2");
        result.RunId.Should().Be("run-2");
        result.Environment.Should().Be("dev");

        _activationRepo.Verify(r => r.UpdateAsync(
            It.Is<GovernanceEnvironmentActivation>(a => !a.IsActive && a.ActivationId == "act-old"),
            It.IsAny<CancellationToken>()), Times.Once);
        _activationRepo.Verify(r => r.CreateAsync(
            It.Is<GovernanceEnvironmentActivation>(a => a.IsActive && a.ManifestVersion == "v2"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Activate_NoExistingActivations_CreatesNewActive()
    {
        _runRepo.Setup(r => r.GetByIdAsync("run-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(ValidRun("run-1"));
        _activationRepo.Setup(r => r.GetByEnvironmentAsync("test", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<GovernanceEnvironmentActivation>());
        _activationRepo.Setup(r => r.CreateAsync(It.IsAny<GovernanceEnvironmentActivation>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await _sut.ActivateAsync("run-1", "v1", "test");

        result.IsActive.Should().BeTrue();
        result.Environment.Should().Be("test");

        _activationRepo.Verify(r => r.UpdateAsync(It.IsAny<GovernanceEnvironmentActivation>(), It.IsAny<CancellationToken>()), Times.Never);
        _activationRepo.Verify(r => r.CreateAsync(It.IsAny<GovernanceEnvironmentActivation>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Activate_RunNotFound_ThrowsRunNotFoundException()
    {
        _runRepo.Setup(r => r.GetByIdAsync("no-such-run", It.IsAny<CancellationToken>()))
            .ReturnsAsync((ArchitectureRun?)null);

        var act = () => _sut.ActivateAsync("no-such-run", "v1", "dev");

        await act.Should().ThrowAsync<RunNotFoundException>()
            .WithMessage("*no-such-run*");
    }
}
