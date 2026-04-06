namespace ArchiForge.Core.Integration;

/// <summary>Logical integration event type strings published to Service Bus (<see cref="IIntegrationEventPublisher"/>).</summary>
/// <remarks>
/// Prefer <c>com.archiforge.*</c> names for alignment with CloudEvents-style reverse-DNS typing.
/// New fields may be added to JSON payloads without bumping the type string; breaking changes require a new type or explicit consumer negotiation.
/// </remarks>
public static class IntegrationEventTypes
{
    public const string AuthorityRunCompletedV1 = "com.archiforge.authority.run.completed";

    public const string GovernanceApprovalSubmittedV1 = "com.archiforge.governance.approval.submitted";

    public const string GovernancePromotionActivatedV1 = "com.archiforge.governance.promotion.activated";

    public const string AlertFiredV1 = "com.archiforge.alert.fired";

    public const string AlertResolvedV1 = "com.archiforge.alert.resolved";

    public const string AdvisoryScanCompletedV1 = "com.archiforge.advisory.scan.completed";
}
