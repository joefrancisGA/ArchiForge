namespace ArchLucid.Core.Integration;

/// <summary>Logical integration event type strings published to Service Bus (<see cref="IIntegrationEventPublisher"/>).</summary>
/// <remarks>Canonical strings use <c>com.archlucid.*</c>.</remarks>
public static class IntegrationEventTypes
{
    public const string AuthorityRunCompletedV1 = "com.archlucid.authority.run.completed";

    public const string GovernanceApprovalSubmittedV1 = "com.archlucid.governance.approval.submitted";

    public const string GovernancePromotionActivatedV1 = "com.archlucid.governance.promotion.activated";

    public const string AlertFiredV1 = "com.archlucid.alert.fired";

    public const string AlertResolvedV1 = "com.archlucid.alert.resolved";

    public const string AdvisoryScanCompletedV1 = "com.archlucid.advisory.scan.completed";

    /// <summary>Wildcard handler: receives every event type after no specific handler matched.</summary>
    public const string WildcardEventType = "*";

    /// <summary>Returns <paramref name="eventType"/> trimmed (no legacy alias mapping).</summary>
    public static string MapToCanonical(string eventType)
    {
        if (string.IsNullOrWhiteSpace(eventType))
        {
            return string.Empty;
        }

        return eventType.Trim();
    }

    /// <summary>True when trimmed type strings match ordinally.</summary>
    public static bool AreEquivalent(string a, string b)
    {
        if (string.IsNullOrWhiteSpace(a) || string.IsNullOrWhiteSpace(b))
        {
            return false;
        }

        return string.Equals(a.Trim(), b.Trim(), StringComparison.Ordinal);
    }
}
