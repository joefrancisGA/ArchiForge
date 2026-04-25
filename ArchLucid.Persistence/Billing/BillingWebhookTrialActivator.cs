using System.Text.Json;

using ArchLucid.Core.Audit;
using ArchLucid.Core.Billing;
using ArchLucid.Core.Diagnostics;
using ArchLucid.Core.Tenancy;

namespace ArchLucid.Persistence.Billing;

/// <summary>Shared post-webhook side effects: ledger activation, tenant conversion, audit.</summary>
public sealed class BillingWebhookTrialActivator(
    IBillingLedger ledger,
    ITenantRepository tenantRepository,
    IAuditService auditService)
{
    private readonly IAuditService
        _auditService = auditService ?? throw new ArgumentNullException(nameof(auditService));

    private readonly IBillingLedger _ledger = ledger ?? throw new ArgumentNullException(nameof(ledger));

    private readonly ITenantRepository _tenantRepository =
        tenantRepository ?? throw new ArgumentNullException(nameof(tenantRepository));

    public async Task OnSubscriptionActivatedAsync(
        Guid tenantId,
        Guid workspaceId,
        Guid projectId,
        string provider,
        string providerSubscriptionId,
        string tierStorageCode,
        string checkoutTierLabel,
        int seats,
        int workspaces,
        string rawWebhookJson,
        CancellationToken cancellationToken)
    {
        await _ledger.ActivateSubscriptionAsync(
            tenantId,
            workspaceId,
            projectId,
            provider,
            providerSubscriptionId,
            tierStorageCode,
            seats,
            workspaces,
            rawWebhookJson,
            cancellationToken);

        TenantTier commercialTier = Enum.TryParse(tierStorageCode, true, out TenantTier parsed)
            ? parsed
            : TenantTier.Standard;

        await _tenantRepository.MarkTrialConvertedAsync(tenantId, commercialTier, cancellationToken);

        ArchLucidInstrumentation.RecordTrialConversion(TrialLifecycleStatus.Active, checkoutTierLabel.Trim());

        string actor = $"billing:{provider}";

        await _auditService.LogAsync(
            new AuditEvent
            {
                EventType = AuditEventTypes.TenantTrialConverted,
                ActorUserId = actor,
                ActorUserName = actor,
                TenantId = tenantId,
                WorkspaceId = workspaceId,
                ProjectId = projectId,
                DataJson = JsonSerializer.Serialize(
                    new { provider, providerSubscriptionId, checkoutTier = checkoutTierLabel })
            },
            cancellationToken);

        ArchLucidInstrumentation.RecordBillingCheckout(provider, checkoutTierLabel.Trim(), "completed");
    }
}
