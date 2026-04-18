using System.Text.Json;

using ArchLucid.Core.Audit;
using ArchLucid.Core.Configuration;
using ArchLucid.Core.Diagnostics;
using ArchLucid.Core.Tenancy;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ArchLucid.Application.Tenancy;

/// <summary>Applies one trial lifecycle step per invocation (Worker scheduler calls this per tenant).</summary>
public sealed class TrialLifecycleTransitionEngine(
    ITenantRepository tenantRepository,
    ITenantHardPurgeService tenantHardPurgeService,
    IAuditService auditService,
    IOptionsMonitor<TrialLifecycleSchedulerOptions> lifecycleOptions,
    TimeProvider timeProvider,
    ILogger<TrialLifecycleTransitionEngine> logger)
{
    private readonly ITenantRepository _tenantRepository =
        tenantRepository ?? throw new ArgumentNullException(nameof(tenantRepository));

    private readonly ITenantHardPurgeService _tenantHardPurgeService =
        tenantHardPurgeService ?? throw new ArgumentNullException(nameof(tenantHardPurgeService));

    private readonly IAuditService _auditService = auditService ?? throw new ArgumentNullException(nameof(auditService));

    private readonly IOptionsMonitor<TrialLifecycleSchedulerOptions> _lifecycleOptions =
        lifecycleOptions ?? throw new ArgumentNullException(nameof(lifecycleOptions));

    private readonly TimeProvider _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));

    private readonly ILogger<TrialLifecycleTransitionEngine> _logger =
        logger ?? throw new ArgumentNullException(nameof(logger));

    /// <summary>Attempts at most one forward transition for <paramref name="tenantId"/>.</summary>
    public async Task<bool> TryAdvanceTenantAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        TenantRecord? tenant = await _tenantRepository.GetByIdAsync(tenantId, cancellationToken);

        if (tenant is null)
        {
            return false;
        }

        TrialLifecycleSchedulerOptions options = _lifecycleOptions.CurrentValue;

        if (string.Equals(tenant.TrialStatus, TrialLifecycleStatus.Deleted, StringComparison.Ordinal))
        {
            TenantHardPurgeResult retry = await _tenantHardPurgeService.PurgeTenantAsync(
                tenantId,
                new TenantHardPurgeOptions
                {
                    DryRun = false,
                    MaxRowsPerStatement = options.HardPurgeMaxRowsPerStatement,
                },
                cancellationToken);

            return retry.RowsDeleted > 0;
        }

        TrialLifecycleAdvancement? advancement = TrialLifecyclePolicy.TryGetNextAdvancement(
            tenant,
            _timeProvider.GetUtcNow(),
            options);

        if (advancement is null)
        {
            return false;
        }

        if (string.Equals(advancement.ToStatus, TrialLifecycleStatus.Deleted, StringComparison.Ordinal))
        {
            bool recorded = await _tenantRepository.TryRecordTrialLifecycleTransitionAsync(
                tenantId,
                advancement.FromStatus,
                advancement.ToStatus,
                advancement.Reason,
                cancellationToken);

            if (!recorded)
            {
                return false;
            }

            await EmitAuditAsync(tenant, advancement, cancellationToken);

            ArchLucidInstrumentation.RecordTrialExpiration($"{advancement.FromStatus}->{advancement.ToStatus}");

            TenantHardPurgeResult purgeResult = await _tenantHardPurgeService.PurgeTenantAsync(
                tenantId,
                new TenantHardPurgeOptions
                {
                    DryRun = false,
                    MaxRowsPerStatement = options.HardPurgeMaxRowsPerStatement,
                },
                cancellationToken);

            _logger.LogInformation(
                "Trial hard purge completed for tenant {TenantId}: rowsDeleted={Rows}.",
                tenantId,
                purgeResult.RowsDeleted);

            return true;
        }

        bool ok = await _tenantRepository.TryRecordTrialLifecycleTransitionAsync(
            tenantId,
            advancement.FromStatus,
            advancement.ToStatus,
            advancement.Reason,
            cancellationToken);

        if (!ok)
        {
            return false;
        }

        await EmitAuditAsync(tenant, advancement, cancellationToken);

        ArchLucidInstrumentation.RecordTrialExpiration($"{advancement.FromStatus}->{advancement.ToStatus}");

        return true;
    }

    private async Task EmitAuditAsync(
        TenantRecord tenant,
        TrialLifecycleAdvancement advancement,
        CancellationToken cancellationToken)
    {
        TenantWorkspaceLink? workspace = await _tenantRepository
            .GetFirstWorkspaceAsync(tenant.Id, cancellationToken)
            .ConfigureAwait(false);

        Guid workspaceId = workspace?.WorkspaceId ?? Guid.Empty;
        Guid projectId = workspace?.DefaultProjectId ?? Guid.Empty;

        await _auditService.LogAsync(
            new AuditEvent
            {
                EventType = AuditEventTypes.TrialLifecycleTransition,
                ActorUserId = "system:trial-lifecycle-scheduler",
                ActorUserName = "system:trial-lifecycle-scheduler",
                TenantId = tenant.Id,
                WorkspaceId = workspaceId,
                ProjectId = projectId,
                DataJson = JsonSerializer.Serialize(
                    new
                    {
                        fromStatus = advancement.FromStatus,
                        toStatus = advancement.ToStatus,
                        reason = advancement.Reason,
                    }),
            },
            cancellationToken);
    }
}
