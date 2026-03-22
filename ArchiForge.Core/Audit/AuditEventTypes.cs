namespace ArchiForge.Core.Audit;

public static class AuditEventTypes
{
    public const string RunStarted = "RunStarted";
    public const string RunCompleted = "RunCompleted";
    public const string ManifestGenerated = "ManifestGenerated";
    public const string ArtifactsGenerated = "ArtifactsGenerated";
    public const string ReplayExecuted = "ReplayExecuted";
    public const string ArtifactDownloaded = "ArtifactDownloaded";
    public const string BundleDownloaded = "BundleDownloaded";
    public const string RunExported = "RunExported";

    public const string RecommendationGenerated = "RecommendationGenerated";
    public const string RecommendationAccepted = "RecommendationAccepted";
    public const string RecommendationRejected = "RecommendationRejected";
    public const string RecommendationDeferred = "RecommendationDeferred";
    public const string RecommendationImplemented = "RecommendationImplemented";

    public const string RecommendationLearningProfileRebuilt = "RecommendationLearningProfileRebuilt";

    public const string AdvisoryScanScheduled = "AdvisoryScanScheduled";
    public const string AdvisoryScanExecuted = "AdvisoryScanExecuted";
    public const string ArchitectureDigestGenerated = "ArchitectureDigestGenerated";

    public const string DigestSubscriptionCreated = "DigestSubscriptionCreated";
    public const string DigestSubscriptionToggled = "DigestSubscriptionToggled";
    public const string DigestDeliverySucceeded = "DigestDeliverySucceeded";
    public const string DigestDeliveryFailed = "DigestDeliveryFailed";

    public const string AlertRuleCreated = "AlertRuleCreated";
    public const string AlertTriggered = "AlertTriggered";
    public const string AlertAcknowledged = "AlertAcknowledged";
    public const string AlertResolved = "AlertResolved";
    public const string AlertSuppressed = "AlertSuppressed";

    public const string AlertRoutingSubscriptionCreated = "AlertRoutingSubscriptionCreated";
    public const string AlertRoutingSubscriptionToggled = "AlertRoutingSubscriptionToggled";
    public const string AlertDeliverySucceeded = "AlertDeliverySucceeded";
    public const string AlertDeliveryFailed = "AlertDeliveryFailed";

    public const string CompositeAlertRuleCreated = "CompositeAlertRuleCreated";
    public const string CompositeAlertTriggered = "CompositeAlertTriggered";
    public const string AlertSuppressedByPolicy = "AlertSuppressedByPolicy";

    public const string AlertRuleSimulationExecuted = "AlertRuleSimulationExecuted";
    public const string AlertRuleCandidateComparisonExecuted = "AlertRuleCandidateComparisonExecuted";
}
