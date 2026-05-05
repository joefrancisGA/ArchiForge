using ArchLucid.Application.Governance;
using ArchLucid.Contracts.Governance;
using ArchLucid.Core.Audit;
using ArchLucid.Persistence.Data.Repositories;

using FluentAssertions;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using Moq;

namespace ArchLucid.Application.Tests.Governance;

[Trait("Suite", "Core")]
[Trait("Category", "Unit")]
public sealed class ApprovalSlaMonitorTests
{
    private readonly InMemoryGovernanceApprovalRequestRepository _approvalRepo = new();
    private readonly Mock<IAuditService> _auditService = new();
    private readonly Mock<IHttpClientFactory> _httpClientFactory = new();
    private readonly ILogger<ApprovalSlaMonitor> _logger = NullLogger<ApprovalSlaMonitor>.Instance;

    private ApprovalSlaMonitor CreateSut(PreCommitGovernanceGateOptions? options = null)
    {
        PreCommitGovernanceGateOptions opts = options ?? new PreCommitGovernanceGateOptions { ApprovalSlaHours = 24, };

        return new ApprovalSlaMonitor(
            _approvalRepo,
            _auditService.Object,
            Options.Create(opts),
            _httpClientFactory.Object,
            _logger);
    }

    [SkippableFact]
    public async Task CheckAndEscalateAsync_emits_audit_for_breached_request()
    {
        GovernanceApprovalRequest request = new()
        {
            RunId = "run-1",
            ManifestVersion = "v1",
            SourceEnvironment = "dev",
            TargetEnvironment = "test",
            Status = GovernanceApprovalStatus.Submitted,
            RequestedBy = "user-a",
            RequestedUtc = DateTime.UtcNow.AddHours(-48),
            SlaDeadlineUtc = DateTime.UtcNow.AddHours(-24),
        };
        await _approvalRepo.CreateAsync(request, CancellationToken.None);

        ApprovalSlaMonitor sut = CreateSut();
        await sut.CheckAndEscalateAsync(CancellationToken.None);

        _auditService.Verify(
            a => a.LogAsync(
                It.Is<AuditEvent>(e => e.EventType == AuditEventTypes.GovernanceApprovalSlaBreached),
                It.IsAny<CancellationToken>()),
            Times.Once);

        GovernanceApprovalRequest? updated = await _approvalRepo.GetByIdAsync(request.ApprovalRequestId, CancellationToken.None);
        updated!.SlaBreachNotifiedUtc.Should().NotBeNull();
    }

    [SkippableFact]
    public async Task CheckAndEscalateAsync_skips_request_before_deadline()
    {
        GovernanceApprovalRequest request = new()
        {
            RunId = "run-2",
            ManifestVersion = "v1",
            SourceEnvironment = "dev",
            TargetEnvironment = "test",
            Status = GovernanceApprovalStatus.Submitted,
            RequestedBy = "user-b",
            RequestedUtc = DateTime.UtcNow,
            SlaDeadlineUtc = DateTime.UtcNow.AddHours(24),
        };
        await _approvalRepo.CreateAsync(request, CancellationToken.None);

        ApprovalSlaMonitor sut = CreateSut();
        await sut.CheckAndEscalateAsync(CancellationToken.None);

        _auditService.Verify(
            a => a.LogAsync(It.IsAny<AuditEvent>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [SkippableFact]
    public async Task CheckAndEscalateAsync_does_not_repeat_for_already_notified_request()
    {
        GovernanceApprovalRequest request = new()
        {
            RunId = "run-3",
            ManifestVersion = "v1",
            SourceEnvironment = "dev",
            TargetEnvironment = "test",
            Status = GovernanceApprovalStatus.Submitted,
            RequestedBy = "user-c",
            RequestedUtc = DateTime.UtcNow.AddHours(-48),
            SlaDeadlineUtc = DateTime.UtcNow.AddHours(-24),
            SlaBreachNotifiedUtc = DateTime.UtcNow.AddHours(-12),
        };
        await _approvalRepo.CreateAsync(request, CancellationToken.None);

        ApprovalSlaMonitor sut = CreateSut();
        await sut.CheckAndEscalateAsync(CancellationToken.None);

        _auditService.Verify(
            a => a.LogAsync(It.IsAny<AuditEvent>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [SkippableFact]
    public async Task CheckAndEscalateAsync_emits_audit_but_no_http_when_no_webhook_url()
    {
        GovernanceApprovalRequest request = new()
        {
            RunId = "run-4",
            ManifestVersion = "v1",
            SourceEnvironment = "dev",
            TargetEnvironment = "test",
            Status = GovernanceApprovalStatus.Submitted,
            RequestedBy = "user-d",
            RequestedUtc = DateTime.UtcNow.AddHours(-48),
            SlaDeadlineUtc = DateTime.UtcNow.AddHours(-24),
        };
        await _approvalRepo.CreateAsync(request, CancellationToken.None);

        PreCommitGovernanceGateOptions opts = new() { ApprovalSlaHours = 24, ApprovalSlaEscalationWebhookUrl = null, };

        ApprovalSlaMonitor sut = CreateSut(opts);
        await sut.CheckAndEscalateAsync(CancellationToken.None);

        _auditService.Verify(
            a => a.LogAsync(
                It.Is<AuditEvent>(e => e.EventType == AuditEventTypes.GovernanceApprovalSlaBreached),
                It.IsAny<CancellationToken>()),
            Times.Once);

        _httpClientFactory.Verify(f => f.CreateClient(It.IsAny<string>()), Times.Never);
    }

    [SkippableFact]
    public async Task CheckAndEscalateAsync_does_nothing_when_sla_not_configured()
    {
        GovernanceApprovalRequest request = new()
        {
            RunId = "run-5",
            ManifestVersion = "v1",
            SourceEnvironment = "dev",
            TargetEnvironment = "test",
            Status = GovernanceApprovalStatus.Submitted,
            RequestedBy = "user-e",
            RequestedUtc = DateTime.UtcNow.AddHours(-48),
            SlaDeadlineUtc = DateTime.UtcNow.AddHours(-24),
        };
        await _approvalRepo.CreateAsync(request, CancellationToken.None);

        PreCommitGovernanceGateOptions opts = new() { ApprovalSlaHours = null, };

        ApprovalSlaMonitor sut = CreateSut(opts);
        await sut.CheckAndEscalateAsync(CancellationToken.None);

        _auditService.Verify(
            a => a.LogAsync(It.IsAny<AuditEvent>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
