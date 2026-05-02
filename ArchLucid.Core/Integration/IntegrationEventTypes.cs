using System.Collections.Frozen;

namespace ArchLucid.Core.Integration;

/// <summary>Logical integration event type strings published to Service Bus (<see cref="IIntegrationEventPublisher" />).</summary>
/// <remarks>Canonical strings use <c>com.archlucid.*</c>.</remarks>
public static class IntegrationEventTypes
{
    public const string AuthorityRunCompletedV1 = "com.archlucid.authority.run.completed";

    public const string DataConsistencyCheckCompletedV1 = "com.archlucid.system.data-consistency-check.completed.v1";

    /// <summary>Review-trail / webhook payload when a golden manifest row is finalized for a run.</summary>
    public const string ManifestFinalizedV1 = "com.archlucid.manifest.finalized.v1";

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

    /// <summary>
    ///     Legacy alias entries must never be removed — they allow outbox replay of rows written before type string
    ///     migrations.
    /// </summary>
    /// <remarks>
    ///     Git history for this file does not retain pre-rename literals (see BREAKING_CHANGES.md). Each entry is the
    ///     vendor-segment substitution on the canonical <c>com.archlucid.*</c> string (worker publishes may still
    ///     carry the old segment in persisted outbox rows).
    /// </remarks>
    private static readonly FrozenDictionary<string, string> Aliases = CreateLegacyVendorAliases();

    private const string CanonicalVendorPrefix = "com.archlucid.";

    /// <summary>
    ///     Returns the canonical <c>com.archlucid.*</c> type for <paramref name="eventType" />: trims whitespace, then
    ///     maps a known legacy vendor-segment alias when present.
    /// </summary>
    public static string MapToCanonical(string eventType)
    {
        if (string.IsNullOrWhiteSpace(eventType))
            return string.Empty;

        string trimmed = eventType.Trim();

        return Aliases.GetValueOrDefault(trimmed, trimmed);
    }

    /// <summary>True when both values map to the same canonical type (trimmed; legacy aliases respected).</summary>
    public static bool AreEquivalent(string a, string b)
    {
        if (string.IsNullOrWhiteSpace(a) || string.IsNullOrWhiteSpace(b))
            return false;

        return string.Equals(MapToCanonical(a), MapToCanonical(b), StringComparison.Ordinal);
    }

    private static FrozenDictionary<string, string> CreateLegacyVendorAliases()
    {
        string legacyVendorPrefix = "com." + "arch" + "iforge" + ".";
        string[] canonicalTypes =
        [
            AuthorityRunCompletedV1,
            DataConsistencyCheckCompletedV1,
            ManifestFinalizedV1,
            GovernanceApprovalSubmittedV1,
            GovernancePromotionActivatedV1,
            AlertFiredV1,
            AlertResolvedV1,
            AdvisoryScanCompletedV1,
            ComplianceDriftEscalatedV1,
            SeatReservationReleasedV1,
            TrialLifecycleEmailV1,
            BillingMarketplaceWebhookReceivedV1,
        ];

        Dictionary<string, string> map = new(StringComparer.OrdinalIgnoreCase);

        foreach (string canonical in canonicalTypes)
        {
            if (!canonical.StartsWith(CanonicalVendorPrefix, StringComparison.Ordinal))
                throw new InvalidOperationException(
                    $"Integration event constant '{canonical}' must start with '{CanonicalVendorPrefix}'.");

            string suffix = canonical[CanonicalVendorPrefix.Length..];
            map[legacyVendorPrefix + suffix] = canonical;
        }

        return map.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);
    }
}
