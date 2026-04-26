namespace ArchLucid.Core.Integration;

/// <summary>Logical integration event type strings published to Service Bus (<see cref="IIntegrationEventPublisher" />).</summary>
/// <remarks>Canonical strings use <c>com.archlucid.*</c>.</remarks>
public static class IntegrationEventTypes
{
    public const string AuthorityRunCompletedV1 = "com.archlucid.authority.run.completed";

    public const string GovernanceApprovalSubmittedV1 = "com.archlucid.governance.approval.submitted";

    public const string GovernancePromotionActivatedV1 = "com.archlucid.governance.promotion.activated";

    public const string AlertFiredV1 = "com.archlucid.alert.fired";

    public const string AlertResolvedV1 = "com.archlucid.alert.resolved";

    public const string AdvisoryScanCompletedV1 = "com.archlucid.advisory.scan.completed";

    /// <summary>
    ///     Compliance drift breached its threshold and escalated (Teams notification trigger added 2026-04-21 per
    ///     PENDING_QUESTIONS.md item 32).
    /// </summary>
    public const string ComplianceDriftEscalatedV1 = "com.archlucid.compliance.drift.escalated";

    /// <summary>
    ///     A trial seat reservation expired or was released, freeing capacity (Teams notification trigger added
    ///     2026-04-21 per PENDING_QUESTIONS.md item 32).
    /// </summary>
    public const string SeatReservationReleasedV1 = "com.archlucid.seat.reservation.released";

    /// <summary>Trial / lifecycle transactional email dispatch (worker consumes JSON payload).</summary>
    public const string TrialLifecycleEmailV1 = "com.archlucid.notifications.trial-lifecycle-email.v1";

    /// <summary>Azure Marketplace SaaS webhook persisted and processed (downstream orchestration / Logic Apps).</summary>
    public const string BillingMarketplaceWebhookReceivedV1 = "com.archlucid.billing.marketplace.webhook.received.v1";

    /// <summary>Wildcard handler: receives every event type after no specific handler matched.</summary>
    public const string WildcardEventType = "*";

    /// <summary>Returns <paramref name="eventType" /> trimmed (no legacy alias mapping).</summary>
    public static string MapToCanonical(string eventType)
    {
        return string.IsNullOrWhiteSpace(eventType) ? string.Empty : eventType.Trim();
    }

    /// <summary>True when trimmed type strings match ordinally.</summary>
    public static bool AreEquivalent(string a, string b)
    {
        if (string.IsNullOrWhiteSpace(a) || string.IsNullOrWhiteSpace(b))
            return false;

        return string.Equals(a.Trim(), b.Trim(), StringComparison.Ordinal);
    }
}
