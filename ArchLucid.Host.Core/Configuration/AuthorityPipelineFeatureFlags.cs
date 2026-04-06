namespace ArchiForge.Host.Core.Configuration;

/// <summary>Feature flag names for authority pipeline rollout (see <c>FeatureManagement</c> configuration).</summary>
public static class AuthorityPipelineFeatureFlags
{
    /// <summary>
    /// When enabled (and storage supports the work outbox), context ingestion and graph resolution run on a background worker.
    /// </summary>
    public const string AsyncAuthorityPipeline = "AsyncAuthorityPipeline";
}
