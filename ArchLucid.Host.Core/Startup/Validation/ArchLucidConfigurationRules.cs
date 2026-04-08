using ArchLucid.Host.Core.Configuration;
using ArchLucid.Host.Core.Hosting;
using ArchLucid.Host.Core.Startup.Validation.Rules;

namespace ArchLucid.Host.Core.Startup.Validation;

/// <summary>
/// Central rules for ArchLucid API configuration. Used for fail-fast startup validation before migrations.
/// </summary>
public static class ArchLucidConfigurationRules
{
    /// <summary>
    /// Collects human-readable configuration errors. Empty list means configuration is acceptable to start the host.
    /// </summary>
    public static IReadOnlyList<string> CollectErrors(
        IConfiguration configuration,
        IWebHostEnvironment environment)
    {
        List<string> errors = [];

        ArchLucidOptions archiForge = ArchLucidConfigurationBridge.ResolveArchLucidOptions(configuration);

        StorageRules.Collect(configuration, archiForge, errors);
        AuthenticationRules.CollectApiKeyWhenEnabled(configuration, errors);
        AgentExecutionRules.Collect(configuration, errors);
        LlmCompletionCacheRules.Collect(configuration, errors);
        SchemaValidationRules.Collect(configuration, errors);
        BatchReplayRules.Collect(configuration, errors);
        ApiDeprecationRules.Collect(configuration, errors);
        DataArchivalRules.Collect(configuration, errors);
        HostLeaderElectionRules.Collect(configuration, errors);
        RetrievalRules.CollectEmbeddingCaps(configuration, errors);
        RetrievalRules.CollectVectorIndex(configuration, errors);
        RateLimitingRules.Collect(configuration, errors);
        HotPathCacheRules.Collect(configuration, environment, errors);
        BackgroundJobsRules.Collect(configuration, errors);
        ObservabilityRules.CollectOtlp(configuration, errors);
        ObservabilityRules.CollectPrometheus(configuration, errors);
        LlmTokenQuotaRules.Collect(configuration, errors);

        if (!environment.IsProduction())
        {
            return errors;
        }

        AuthenticationRules.CollectProductionApiKeyBypass(configuration, errors);

        ArchLucidHostingRole hostingRole = HostingRoleResolver.Resolve(configuration);

        if (hostingRole == ArchLucidHostingRole.Worker)
        {
            ProductionSafetyRules.CollectWebhookSecrets(configuration, errors);
            ProductionSafetyRules.CollectSqlRowLevelSecurity(configuration, archiForge, errors);

            return errors;
        }

        ProductionSafetyRules.CollectCors(configuration, errors);
        ProductionSafetyRules.CollectWebhookSecrets(configuration, errors);
        ProductionSafetyRules.CollectSqlRowLevelSecurity(configuration, archiForge, errors);
        AuthenticationRules.CollectProductionAuthModes(configuration, errors);

        return errors;
    }
}
