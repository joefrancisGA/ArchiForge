using ArchLucid.Core.Audit;
using ArchLucid.Core.Tenancy;
using ArchLucid.Persistence.Tenancy;

using Moq;

namespace ArchLucid.Persistence.Tests.Tenancy;

[Trait("Category", "Unit")]
[Trait("Suite", "Persistence")]
public sealed class SqlTrialFunnelCommitHookTests
{
    [SkippableFact]
    public async Task OnTrialTenantManifestCommittedAsync_when_column_pin_succeeds_for_non_trial_tenant_does_not_log_trial_audit()
    {
        Guid tenantId = Guid.NewGuid();
        DateTimeOffset committed = DateTimeOffset.Parse("2026-04-10T12:00:00+00:00");
        Mock<ITenantRepository> tenants = new();
        tenants
            .Setup(t => t.TryMarkFirstManifestCommittedAsync(tenantId, committed, It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                new TrialFirstManifestCommitOutcome { SignupToCommitSeconds = 10, TrialRunUsageRatio = 0.2 });
        tenants.Setup(t => t.GetByIdAsync(tenantId, It.IsAny<CancellationToken>())).ReturnsAsync(
            new TenantRecord
            {
                Id = tenantId,
                Name = "Paid",
                Slug = "paid",
                Tier = TenantTier.Standard,
                CreatedUtc = DateTimeOffset.Parse("2026-01-01T00:00:00+00:00"),
                TrialRunsUsed = 0,
                TrialSeatsUsed = 0,
                TrialExpiresUtc = null,
                TrialStatus = null
            });
        Mock<IAuditService> audit = new();
        SqlTrialFunnelCommitHook sut = new(tenants.Object, audit.Object);

        await sut.OnTrialTenantManifestCommittedAsync(tenantId, committed, CancellationToken.None);

        audit.Verify(
            a => a.LogAsync(It.IsAny<AuditEvent>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [SkippableFact]
    public async Task OnTrialTenantManifestCommittedAsync_when_trial_tenant_logs_TrialFirstRunCompleted()
    {
        Guid tenantId = Guid.NewGuid();
        Guid workspaceId = Guid.NewGuid();
        Guid projectId = Guid.NewGuid();
        DateTimeOffset committed = DateTimeOffset.Parse("2026-04-10T12:00:00+00:00");
        Mock<ITenantRepository> tenants = new();
        tenants
            .Setup(t => t.TryMarkFirstManifestCommittedAsync(tenantId, committed, It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                new TrialFirstManifestCommitOutcome { SignupToCommitSeconds = 3600, TrialRunUsageRatio = 0.5 });
        tenants.Setup(t => t.GetByIdAsync(tenantId, It.IsAny<CancellationToken>())).ReturnsAsync(
            new TenantRecord
            {
                Id = tenantId,
                Name = "Trial",
                Slug = "trial",
                Tier = TenantTier.Free,
                CreatedUtc = DateTimeOffset.Parse("2026-04-01T00:00:00+00:00"),
                TrialRunsUsed = 1,
                TrialSeatsUsed = 0,
                TrialExpiresUtc = DateTimeOffset.Parse("2026-05-01T00:00:00+00:00"),
                TrialStatus = TrialLifecycleStatus.Active,
                TrialStartUtc = DateTimeOffset.Parse("2026-04-01T00:00:00+00:00")
            });
        tenants
            .Setup(t => t.GetFirstWorkspaceAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                new TenantWorkspaceLink { WorkspaceId = workspaceId, DefaultProjectId = projectId });
        Mock<IAuditService> audit = new();
        SqlTrialFunnelCommitHook sut = new(tenants.Object, audit.Object);

        await sut.OnTrialTenantManifestCommittedAsync(tenantId, committed, CancellationToken.None);

        audit.Verify(
            a => a.LogAsync(
                It.Is<AuditEvent>(e => e.EventType == AuditEventTypes.TrialFirstRunCompleted),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
