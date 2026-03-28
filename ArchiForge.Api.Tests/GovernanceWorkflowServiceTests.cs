using ArchiForge.Application;
using ArchiForge.Application.Governance;
using ArchiForge.Contracts.Architecture;
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
    private readonly Mock<IRunDetailQueryService> _runDetailQueryService;
    private readonly Mock<IBaselineMutationAuditService> _baselineAudit;
    private readonly GovernanceWorkflowService _sut;

    public GovernanceWorkflowServiceTests()
    {
        _approvalRepo = new Mock<IGovernanceApprovalRequestRepository>();
        _promotionRepo = new Mock<IGovernancePromotionRecordRepository>();
        _activationRepo = new Mock<IGovernanceEnvironmentActivationRepository>();
        _runDetailQueryService = new Mock<IRunDetailQueryService>();
        _baselineAudit = new Mock<IBaselineMutationAuditService>();

        Mock<ILogger<GovernanceWorkflowService>> logger = new();

        _sut = new GovernanceWorkflowService(
            _approvalRepo.Object,
            _promotionRepo.Object,
            _activationRepo.Object,
            _runDetailQueryService.Object,
            _baselineAudit.Object,
            logger.Object);
    }

    private static ArchitectureRun ValidRun(string runId = "run-1") => new()
    {
        RunId = runId,
        RequestId = "req-1",
        Status = ArchitectureRunStatus.Committed,
        CreatedUtc = DateTime.UtcNow
    };

    private static ArchitectureRunDetail DetailForRun(string runId) => new() { Run = ValidRun(runId) };

    // ── Submit ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task SubmitApprovalRequest_RunExists_CreatesSubmittedRequest()
    {
        _runDetailQueryService.Setup(s => s.GetRunDetailAsync("run-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(DetailForRun("run-1"));
        _approvalRepo.Setup(r => r.CreateAsync(It.IsAny<GovernanceApprovalRequest>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        GovernanceApprovalRequest result = await _sut.SubmitApprovalRequestAsync(
            "run-1", "v1", "dev", "test", "alice", null);

        result.Status.Should().Be(GovernanceApprovalStatus.Submitted);
        result.RunId.Should().Be("run-1");
        result.ManifestVersion.Should().Be("v1");
        result.SourceEnvironment.Should().Be("dev");
        result.TargetEnvironment.Should().Be("test");
        result.RequestedBy.Should().Be("alice");

        _approvalRepo.Verify(r => r.CreateAsync(It.IsAny<GovernanceApprovalRequest>(), It.IsAny<CancellationToken>()), Times.Once);

        _baselineAudit.Verify(
            a => a.RecordAsync(
                AuditEventTypes.Governance.ApprovalRequestSubmitted,
                "alice",
                It.IsAny<string>(),
                It.Is<string>(d => d.Contains("run-1", StringComparison.Ordinal) && d.Contains("v1", StringComparison.Ordinal)),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task SubmitApprovalRequest_RunNotFound_ThrowsRunNotFoundException()
    {
        _runDetailQueryService.Setup(s => s.GetRunDetailAsync("missing-run", It.IsAny<CancellationToken>()))
            .ReturnsAsync((ArchitectureRunDetail?)null);

        Func<Task<GovernanceApprovalRequest>> act = () => _sut.SubmitApprovalRequestAsync(
            "missing-run", "v1", "dev", "test", "alice", null);

        await act.Should().ThrowAsync<RunNotFoundException>()
            .WithMessage("*missing-run*");

        _baselineAudit.Verify(
            a => a.RecordAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // ── Approve ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task Approve_SubmittedRequest_ChangesStatusToApproved()
    {
        GovernanceApprovalRequest existing = new()
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

        GovernanceApprovalRequest result = await _sut.ApproveAsync("apr-1", "bob", "LGTM");

        result.Status.Should().Be(GovernanceApprovalStatus.Approved);
        result.ReviewedBy.Should().Be("bob");
        result.ReviewComment.Should().Be("LGTM");
        result.ReviewedUtc.Should().NotBeNull();

        _approvalRepo.Verify(r => r.UpdateAsync(
            It.Is<GovernanceApprovalRequest>(x => x.Status == GovernanceApprovalStatus.Approved),
            It.IsAny<CancellationToken>()), Times.Once);

        _baselineAudit.Verify(
            a => a.RecordAsync(
                AuditEventTypes.Governance.ApprovalRequestApproved,
                "bob",
                "apr-1",
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Approve_DraftRequest_ChangesStatusToApproved()
    {
        GovernanceApprovalRequest existing = new()
        {
            ApprovalRequestId = "apr-draft",
            Status = GovernanceApprovalStatus.Draft
        };

        _approvalRepo.Setup(r => r.GetByIdAsync("apr-draft", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);
        _approvalRepo.Setup(r => r.UpdateAsync(It.IsAny<GovernanceApprovalRequest>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        GovernanceApprovalRequest result = await _sut.ApproveAsync("apr-draft", "bob", null);

        result.Status.Should().Be(GovernanceApprovalStatus.Approved);
    }

    [Fact]
    public async Task Approve_AlreadyRejected_ThrowsInvalidOperationException()
    {
        GovernanceApprovalRequest existing = new()
        {
            ApprovalRequestId = "apr-rejected",
            Status = GovernanceApprovalStatus.Rejected
        };

        _approvalRepo.Setup(r => r.GetByIdAsync("apr-rejected", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        Func<Task<GovernanceApprovalRequest>> act = () => _sut.ApproveAsync("apr-rejected", "bob", null);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*cannot be approved*");

        _baselineAudit.Verify(
            a => a.RecordAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // ── Reject ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task Reject_SubmittedRequest_ChangesStatusToRejected()
    {
        GovernanceApprovalRequest existing = new()
        {
            ApprovalRequestId = "apr-2",
            Status = GovernanceApprovalStatus.Submitted
        };

        _approvalRepo.Setup(r => r.GetByIdAsync("apr-2", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);
        _approvalRepo.Setup(r => r.UpdateAsync(It.IsAny<GovernanceApprovalRequest>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        GovernanceApprovalRequest result = await _sut.RejectAsync("apr-2", "carol", "Needs more detail");

        result.Status.Should().Be(GovernanceApprovalStatus.Rejected);
        result.ReviewedBy.Should().Be("carol");
        result.ReviewComment.Should().Be("Needs more detail");

        _baselineAudit.Verify(
            a => a.RecordAsync(
                AuditEventTypes.Governance.ApprovalRequestRejected,
                "carol",
                "apr-2",
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Reject_AlreadyApproved_ThrowsInvalidOperationException()
    {
        GovernanceApprovalRequest existing = new()
        {
            ApprovalRequestId = "apr-approved",
            Status = GovernanceApprovalStatus.Approved
        };

        _approvalRepo.Setup(r => r.GetByIdAsync("apr-approved", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        Func<Task<GovernanceApprovalRequest>> act = () => _sut.RejectAsync("apr-approved", "carol", null);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*cannot be rejected*");

        _baselineAudit.Verify(
            a => a.RecordAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // ── Promote ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task Promote_ToProd_WithoutApproval_ThrowsInvalidOperationException()
    {
        _runDetailQueryService.Setup(s => s.GetRunDetailAsync("run-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(DetailForRun("run-1"));

        Func<Task<GovernancePromotionRecord>> act = () => _sut.PromoteAsync(
            "run-1", "v1", "test", GovernanceEnvironment.Prod,
            "alice", null, null);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*approved approval request*");

        _baselineAudit.Verify(
            a => a.RecordAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Promote_ToProd_WithUnapprovedRequest_ThrowsInvalidOperationException()
    {
        GovernanceApprovalRequest pendingApproval = new()
        {
            ApprovalRequestId = "apr-pending",
            Status = GovernanceApprovalStatus.Submitted
        };

        _runDetailQueryService.Setup(s => s.GetRunDetailAsync("run-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(DetailForRun("run-1"));
        _approvalRepo.Setup(r => r.GetByIdAsync("apr-pending", It.IsAny<CancellationToken>()))
            .ReturnsAsync(pendingApproval);

        Func<Task<GovernancePromotionRecord>> act = () => _sut.PromoteAsync(
            "run-1", "v1", "test", GovernanceEnvironment.Prod,
            "alice", "apr-pending", null);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*approved approval request*");

        _baselineAudit.Verify(
            a => a.RecordAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Promote_ToProd_WithApprovedRequest_Succeeds()
    {
        GovernanceApprovalRequest approvedRequest = new()
        {
            ApprovalRequestId = "apr-approved",
            Status = GovernanceApprovalStatus.Approved,
            RunId = "run-1",
            ManifestVersion = "v1",
            TargetEnvironment = GovernanceEnvironment.Prod
        };

        _runDetailQueryService.Setup(s => s.GetRunDetailAsync("run-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(DetailForRun("run-1"));
        _approvalRepo.Setup(r => r.GetByIdAsync("apr-approved", It.IsAny<CancellationToken>()))
            .ReturnsAsync(approvedRequest);
        _approvalRepo.Setup(r => r.UpdateAsync(It.IsAny<GovernanceApprovalRequest>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _promotionRepo.Setup(r => r.CreateAsync(It.IsAny<GovernancePromotionRecord>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        GovernancePromotionRecord result = await _sut.PromoteAsync(
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

        _baselineAudit.Verify(
            a => a.RecordAsync(
                AuditEventTypes.Governance.ManifestPromoted,
                "alice",
                It.IsAny<string>(),
                It.Is<string>(d => d.Contains("run-1", StringComparison.Ordinal) && d.Contains("prod", StringComparison.OrdinalIgnoreCase)),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Promote_ToTest_WithoutApproval_Succeeds()
    {
        _runDetailQueryService.Setup(s => s.GetRunDetailAsync("run-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(DetailForRun("run-1"));
        _promotionRepo.Setup(r => r.CreateAsync(It.IsAny<GovernancePromotionRecord>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        GovernancePromotionRecord result = await _sut.PromoteAsync(
            "run-1", "v1", "dev", GovernanceEnvironment.Test,
            "alice", null, null);

        result.TargetEnvironment.Should().Be(GovernanceEnvironment.Test);
        _promotionRepo.Verify(r => r.CreateAsync(It.IsAny<GovernancePromotionRecord>(), It.IsAny<CancellationToken>()), Times.Once);

        _baselineAudit.Verify(
            a => a.RecordAsync(
                AuditEventTypes.Governance.ManifestPromoted,
                "alice",
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // ── Activate ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task Activate_DeactivatesPreviousActiveActivation()
    {
        GovernanceEnvironmentActivation previous = new()
        {
            ActivationId = "act-old",
            RunId = "run-1",
            ManifestVersion = "v1",
            Environment = "dev",
            IsActive = true
        };

        _runDetailQueryService.Setup(s => s.GetRunDetailAsync("run-2", It.IsAny<CancellationToken>()))
            .ReturnsAsync(DetailForRun("run-2"));
        _activationRepo.Setup(r => r.GetByEnvironmentAsync("dev", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<GovernanceEnvironmentActivation> { previous });
        _activationRepo.Setup(r => r.UpdateAsync(It.IsAny<GovernanceEnvironmentActivation>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _activationRepo.Setup(r => r.CreateAsync(It.IsAny<GovernanceEnvironmentActivation>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        GovernanceEnvironmentActivation result = await _sut.ActivateAsync("run-2", "v2", "dev", "activator");

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

        _baselineAudit.Verify(
            a => a.RecordAsync(
                AuditEventTypes.Governance.EnvironmentActivated,
                "activator",
                It.IsAny<string>(),
                It.Is<string>(d => d.Contains("run-2", StringComparison.Ordinal) && d.Contains("dev", StringComparison.Ordinal)),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Activate_NoExistingActivations_CreatesNewActive()
    {
        _runDetailQueryService.Setup(s => s.GetRunDetailAsync("run-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(DetailForRun("run-1"));
        _activationRepo.Setup(r => r.GetByEnvironmentAsync("test", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<GovernanceEnvironmentActivation>());
        _activationRepo.Setup(r => r.CreateAsync(It.IsAny<GovernanceEnvironmentActivation>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        GovernanceEnvironmentActivation result = await _sut.ActivateAsync("run-1", "v1", "test", "activator");

        result.IsActive.Should().BeTrue();
        result.Environment.Should().Be("test");

        _activationRepo.Verify(r => r.UpdateAsync(It.IsAny<GovernanceEnvironmentActivation>(), It.IsAny<CancellationToken>()), Times.Never);
        _activationRepo.Verify(r => r.CreateAsync(It.IsAny<GovernanceEnvironmentActivation>(), It.IsAny<CancellationToken>()), Times.Once);

        _baselineAudit.Verify(
            a => a.RecordAsync(
                AuditEventTypes.Governance.EnvironmentActivated,
                "activator",
                It.IsAny<string>(),
                It.Is<string>(d => d.Contains("run-1", StringComparison.Ordinal) && d.Contains("test", StringComparison.Ordinal)),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Activate_RunNotFound_ThrowsRunNotFoundException()
    {
        _runDetailQueryService.Setup(s => s.GetRunDetailAsync("no-such-run", It.IsAny<CancellationToken>()))
            .ReturnsAsync((ArchitectureRunDetail?)null);

        Func<Task<GovernanceEnvironmentActivation>> act = () => _sut.ActivateAsync("no-such-run", "v1", "dev", "activator");

        await act.Should().ThrowAsync<RunNotFoundException>()
            .WithMessage("*no-such-run*");

        _baselineAudit.Verify(
            a => a.RecordAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
