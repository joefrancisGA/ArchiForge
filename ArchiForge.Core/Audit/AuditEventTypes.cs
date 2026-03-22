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
}
