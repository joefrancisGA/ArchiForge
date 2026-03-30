namespace ArchiForge.Api.Startup.Diagnostics;

/// <summary>
/// Non-secret effective configuration surfaced once at startup for pilot support and diagnostics.
/// </summary>
public sealed record StartupConfigurationFacts(
    string HostEnvironmentName,
    bool SqlConnectionStringConfigured,
    string ArchiForgeStorageProvider,
    string RetrievalVectorIndex,
    string AgentExecutionMode,
    string ArchiForgeAuthMode,
    bool AuthenticationApiKeyEnabled,
    bool AuthenticationApiKeyAdminConfigured,
    bool AuthenticationApiKeyReadOnlyConfigured,
    int CorsAllowedOriginCount,
    int RateLimitingFixedWindowPermitLimit,
    bool ObservabilityPrometheusEnabled,
    bool DemoEnabled,
    bool DemoSeedOnStartup,
    bool SchemaValidationEnableDetailedErrors);

internal static class StartupConfigurationFactsReader
{
    public static StartupConfigurationFacts FromConfiguration(
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        IConfigurationSection corsOrigins = configuration.GetSection("Cors:AllowedOrigins");
        int corsCount = corsOrigins.GetChildren().Count();

        int rateLimit = configuration.GetValue("RateLimiting:FixedWindow:PermitLimit", 0);

        return new StartupConfigurationFacts(
            environment.EnvironmentName,
            !string.IsNullOrWhiteSpace(configuration.GetConnectionString("ArchiForge")),
            configuration["ArchiForge:StorageProvider"] ?? "(missing)",
            configuration["Retrieval:VectorIndex"] ?? "(missing)",
            configuration["AgentExecution:Mode"] ?? "(missing)",
            configuration["ArchiForgeAuth:Mode"] ?? "(missing)",
            configuration.GetValue("Authentication:ApiKey:Enabled", false),
            !string.IsNullOrWhiteSpace(configuration["Authentication:ApiKey:AdminKey"]),
            !string.IsNullOrWhiteSpace(configuration["Authentication:ApiKey:ReadOnlyKey"]),
            corsCount,
            rateLimit,
            configuration.GetValue("Observability:Prometheus:Enabled", false),
            configuration.GetValue("Demo:Enabled", false),
            configuration.GetValue("Demo:SeedOnStartup", false),
            configuration.GetValue("SchemaValidation:EnableDetailedErrors", false));
    }
}
