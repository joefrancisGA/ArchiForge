using System.Text.Json;

using ArchLucid.Core.Audit;
using ArchLucid.Core.Diagnostics;
using ArchLucid.Core.Tenancy;

namespace ArchLucid.Persistence.Tenancy;

/// <summary>Records first-manifest trial funnel latency, usage ratio metrics, and <see cref="AuditEventTypes.TrialFirstRunCompleted"/>.</summary>
public sealed class SqlTrialFunnelCommitHook(ITenantRepository tenantRepository, IAuditService auditService)
    : ITrialFunnelCommitHook
{
    private readonly ITenantRepository _tenantRepository =
        tenantRepository ?? throw new ArgumentNullException(nameof(tenantRepository));

    private readonly IAuditService _auditService = auditService ?? throw new ArgumentNullException(nameof(auditService));

    public async Task OnTrialTenantManifestCommittedAsync(
        Guid tenantId,
        DateTimeOffset committedUtc,
        CancellationToken cancellationToken)
    {
        TrialFirstManifestCommitOutcome? outcome =
            await _tenantRepository.TryMarkTrialFirstManifestCommittedAsync(tenantId, committedUtc, cancellationToken)
                .ConfigureAwait(false);

        if (outcome is null)
        {
            return;
        }

        ArchLucidInstrumentation.RecordTrialFirstRunLatencySeconds(outcome.SignupToCommitSeconds);
        ArchLucidInstrumentation.RecordTrialRunsUsedRatio(outcome.TrialRunUsageRatio);

        TenantRecord? tenant = await _tenantRepository.GetByIdAsync(tenantId, cancellationToken).ConfigureAwait(false);

        if (tenant is null)
        {
            return;
        }

        TenantWorkspaceLink? workspace = await _tenantRepository
            .GetFirstWorkspaceAsync(tenantId, cancellationToken)
            .ConfigureAwait(false);

        Guid workspaceId = workspace?.WorkspaceId ?? Guid.Empty;
        Guid projectId = workspace?.DefaultProjectId ?? Guid.Empty;

        await _auditService.LogAsync(
            new AuditEvent
            {
                EventType = AuditEventTypes.TrialFirstRunCompleted,
                ActorUserId = "system:trial-funnel",
                ActorUserName = "system:trial-funnel",
                TenantId = tenantId,
                WorkspaceId = workspaceId,
                ProjectId = projectId,
                DataJson = JsonSerializer.Serialize(
                    new
                    {
                        signupToCommitSeconds = outcome.SignupToCommitSeconds,
                        trialRunUsageRatio = outcome.TrialRunUsageRatio,
                    }),
            },
            cancellationToken).ConfigureAwait(false);
    }
}
