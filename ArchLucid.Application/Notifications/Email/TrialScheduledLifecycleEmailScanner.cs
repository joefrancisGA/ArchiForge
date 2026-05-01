using System.Globalization;

using ArchLucid.Core.Configuration;
using ArchLucid.Core.Integration;
using ArchLucid.Core.Tenancy;
using ArchLucid.Persistence;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ArchLucid.Application.Notifications.Email;

/// <summary>
///     Worker-only scan that enqueues trial lifecycle email integration events (idempotent sends happen in the
///     dispatcher).
/// </summary>
public sealed class TrialScheduledLifecycleEmailScanner(
    ITenantRepository tenantRepository,
    IIntegrationEventOutboxRepository outboxRepository,
    IIntegrationEventPublisher integrationEventPublisher,
    IOptionsMonitor<IntegrationEventsOptions> integrationEventsOptions,
    IOptionsMonitor<TrialLifecycleEmailRoutingOptions> trialLifecycleEmailRoutingOptions,
    ILogger<TrialScheduledLifecycleEmailScanner> logger)
{
    private readonly IIntegrationEventPublisher _integrationEventPublisher =
        integrationEventPublisher ?? throw new ArgumentNullException(nameof(integrationEventPublisher));

    private readonly IOptionsMonitor<IntegrationEventsOptions> _integrationEventsOptions =
        integrationEventsOptions ?? throw new ArgumentNullException(nameof(integrationEventsOptions));

    private readonly ILogger<TrialScheduledLifecycleEmailScanner> _logger =
        logger ?? throw new ArgumentNullException(nameof(logger));

    private readonly IIntegrationEventOutboxRepository _outboxRepository =
        outboxRepository ?? throw new ArgumentNullException(nameof(outboxRepository));

    private readonly ITenantRepository _tenantRepository =
        tenantRepository ?? throw new ArgumentNullException(nameof(tenantRepository));

    private readonly IOptionsMonitor<TrialLifecycleEmailRoutingOptions> _trialLifecycleEmailRoutingOptions =
        trialLifecycleEmailRoutingOptions ?? throw new ArgumentNullException(nameof(trialLifecycleEmailRoutingOptions));

    public async Task PublishDueAsync(DateTimeOffset utcNow, CancellationToken cancellationToken)
    {
        if (_trialLifecycleEmailRoutingOptions.CurrentValue.IsLogicAppOwned())
        {
            if (_logger.IsEnabled(LogLevel.Debug))

                _logger.LogDebug(
                    "Trial scheduled lifecycle email scan skipped ({Owner}={LogicApp}).",
                    nameof(TrialLifecycleEmailRoutingOptions.Owner),
                    TrialLifecycleEmailRoutingOptions.OwnerModes.LogicApp);

            return;
        }

        IntegrationEventsOptions options = _integrationEventsOptions.CurrentValue;
        IReadOnlyList<TenantRecord>
            tenants = await _tenantRepository.ListAsync(cancellationToken).ConfigureAwait(false);

        foreach (TenantRecord tenant in tenants)
        {
            if (!string.Equals(tenant.TrialStatus, TrialLifecycleStatus.Active, StringComparison.Ordinal))
                continue;

            TenantWorkspaceLink? workspaceLink = await _tenantRepository
                .GetFirstWorkspaceAsync(tenant.Id, cancellationToken)
                .ConfigureAwait(false);

            if (workspaceLink is null)
                continue;

            await TryPublishMidTrialAsync(tenant, workspaceLink, utcNow, options, cancellationToken)
                .ConfigureAwait(false);
            await TryPublishApproachingLimitAsync(tenant, workspaceLink, utcNow, options, cancellationToken)
                .ConfigureAwait(false);
            await TryPublishExpiringSoonAsync(tenant, workspaceLink, utcNow, options, cancellationToken)
                .ConfigureAwait(false);
            await TryPublishExpiredAsync(tenant, workspaceLink, utcNow, options, cancellationToken)
                .ConfigureAwait(false);
        }
    }

    private async Task TryPublishMidTrialAsync(
        TenantRecord tenant,
        TenantWorkspaceLink workspaceLink,
        DateTimeOffset utcNow,
        IntegrationEventsOptions options,
        CancellationToken cancellationToken)
    {
        if (tenant.TrialStartUtc is null)
            return;

        if (tenant.TrialExpiresUtc is { } exp && exp <= utcNow)
            return;

        if ((utcNow - tenant.TrialStartUtc.Value).TotalDays < 7d)
            return;

        TrialLifecycleEmailIntegrationEnvelope envelope = new()
        {
            SchemaVersion = 1,
            Trigger = TrialLifecycleEmailTrigger.MidTrialDay7,
            TenantId = tenant.Id,
            WorkspaceId = workspaceLink.WorkspaceId,
            ProjectId = workspaceLink.DefaultProjectId,
            RunId = null
        };

        string dayBucket = utcNow.ToString("yyyyMMdd", CultureInfo.InvariantCulture);
        string messageId = $"trial-email-scan|{tenant.Id:N}|{TrialLifecycleEmailTrigger.MidTrialDay7}|{dayBucket}";

        await TrialLifecycleIntegrationEventPublisher
            .TryPublishAsync(
                _outboxRepository,
                _integrationEventPublisher,
                options,
                _logger,
                envelope,
                messageId,
                cancellationToken)
            .ConfigureAwait(false);
    }

    private async Task TryPublishApproachingLimitAsync(
        TenantRecord tenant,
        TenantWorkspaceLink workspaceLink,
        DateTimeOffset utcNow,
        IntegrationEventsOptions options,
        CancellationToken cancellationToken)
    {
        if (tenant.TrialRunsLimit is not ({ } limit and > 0))
            return;

        int threshold = (int)Math.Ceiling(limit * 0.8d);

        if (tenant.TrialRunsUsed < threshold)
            return;

        TrialLifecycleEmailIntegrationEnvelope envelope = new()
        {
            SchemaVersion = 1,
            Trigger = TrialLifecycleEmailTrigger.ApproachingRunLimit,
            TenantId = tenant.Id,
            WorkspaceId = workspaceLink.WorkspaceId,
            ProjectId = workspaceLink.DefaultProjectId,
            RunId = null
        };

        string dayBucket = utcNow.ToString("yyyyMMdd", CultureInfo.InvariantCulture);
        string messageId =
            $"trial-email-scan|{tenant.Id:N}|{TrialLifecycleEmailTrigger.ApproachingRunLimit}|{dayBucket}";

        await TrialLifecycleIntegrationEventPublisher
            .TryPublishAsync(
                _outboxRepository,
                _integrationEventPublisher,
                options,
                _logger,
                envelope,
                messageId,
                cancellationToken)
            .ConfigureAwait(false);
    }

    private async Task TryPublishExpiringSoonAsync(
        TenantRecord tenant,
        TenantWorkspaceLink workspaceLink,
        DateTimeOffset utcNow,
        IntegrationEventsOptions options,
        CancellationToken cancellationToken)
    {
        if (tenant.TrialExpiresUtc is not { } exp)
            return;

        if (exp <= utcNow)
            return;

        if ((exp - utcNow).TotalDays > 2d)
            return;

        TrialLifecycleEmailIntegrationEnvelope envelope = new()
        {
            SchemaVersion = 1,
            Trigger = TrialLifecycleEmailTrigger.ExpiringSoon,
            TenantId = tenant.Id,
            WorkspaceId = workspaceLink.WorkspaceId,
            ProjectId = workspaceLink.DefaultProjectId,
            RunId = null
        };

        string dayBucket = utcNow.ToString("yyyyMMdd", CultureInfo.InvariantCulture);
        string messageId = $"trial-email-scan|{tenant.Id:N}|{TrialLifecycleEmailTrigger.ExpiringSoon}|{dayBucket}";

        await TrialLifecycleIntegrationEventPublisher
            .TryPublishAsync(
                _outboxRepository,
                _integrationEventPublisher,
                options,
                _logger,
                envelope,
                messageId,
                cancellationToken)
            .ConfigureAwait(false);
    }

    private async Task TryPublishExpiredAsync(
        TenantRecord tenant,
        TenantWorkspaceLink workspaceLink,
        DateTimeOffset utcNow,
        IntegrationEventsOptions options,
        CancellationToken cancellationToken)
    {
        if (tenant.TrialExpiresUtc is not { } exp)
            return;

        if (exp > utcNow)
            return;

        TrialLifecycleEmailIntegrationEnvelope envelope = new()
        {
            SchemaVersion = 1,
            Trigger = TrialLifecycleEmailTrigger.Expired,
            TenantId = tenant.Id,
            WorkspaceId = workspaceLink.WorkspaceId,
            ProjectId = workspaceLink.DefaultProjectId,
            RunId = null
        };

        string dayBucket = utcNow.ToString("yyyyMMdd", CultureInfo.InvariantCulture);
        string messageId = $"trial-email-scan|{tenant.Id:N}|{TrialLifecycleEmailTrigger.Expired}|{dayBucket}";

        await TrialLifecycleIntegrationEventPublisher
            .TryPublishAsync(
                _outboxRepository,
                _integrationEventPublisher,
                options,
                _logger,
                envelope,
                messageId,
                cancellationToken)
            .ConfigureAwait(false);
    }
}
