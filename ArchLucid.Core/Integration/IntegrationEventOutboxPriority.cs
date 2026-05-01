namespace ArchLucid.Core.Integration;

/// <summary>
///     Maps integration event logical types to <c>dbo.IntegrationEventOutbox.Priority</c> dequeue buckets:
///     0 = critical (operators / compliance), 1 = standard external, 2 = internal worker dispatch.
/// </summary>
public static class IntegrationEventOutboxPriority
{
    /// <returns>0 (critical), 1 (standard), or 2 (internal).</returns>
    public static int ForEventType(string? eventType)
    {
        if (eventType is null)
            return 1;

        string trimmed = eventType.Trim();

        if (string.IsNullOrEmpty(trimmed))
            return 1;

        string canonical = IntegrationEventTypes.MapToCanonical(trimmed);

        if (canonical.Length is 0)
            return 1;

        return TierFromCanonical(canonical);
    }

    private static int TierFromCanonical(string canonical) => canonical switch
    {
        IntegrationEventTypes.AlertFiredV1 or IntegrationEventTypes.AlertResolvedV1
            or IntegrationEventTypes.ComplianceDriftEscalatedV1 => 0,

        IntegrationEventTypes.TrialLifecycleEmailV1 => 2,

        IntegrationEventTypes.AuthorityRunCompletedV1
            or IntegrationEventTypes.ManifestFinalizedV1
            or IntegrationEventTypes.GovernanceApprovalSubmittedV1
            or IntegrationEventTypes.GovernancePromotionActivatedV1
            or IntegrationEventTypes.AdvisoryScanCompletedV1
            or IntegrationEventTypes.SeatReservationReleasedV1
            or IntegrationEventTypes.BillingMarketplaceWebhookReceivedV1
            or IntegrationEventTypes.DataConsistencyCheckCompletedV1 => 1,

        _ => 1,
    };
}
