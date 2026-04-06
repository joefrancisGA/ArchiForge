using System.Reflection;

using ArchiForge.Core.Diagnostics;

namespace ArchiForge.Host.Core.Startup.Diagnostics;

/// <summary>
/// Non-secret effective configuration surfaced once at startup for pilot support and diagnostics.
/// </summary>
/// <param name="BuildCommitSha">Parsed commit from informational version when SourceRevisionId was set at build; otherwise null.</param>
public sealed record StartupConfigurationFacts(
    string HostEnvironmentName,
    string ContentRootPath,
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
    bool SchemaValidationEnableDetailedErrors,
    string BuildInformationalVersion,
    string BuildAssemblyVersion,
    string? BuildFileVersion,
    string? BuildCommitSha,
    string RuntimeFrameworkDescription);

internal static class StartupConfigurationFactsReader
{
    public static StartupConfigurationFacts FromConfiguration(
        IConfiguration configuration,
        IHostEnvironment environment,
        Assembly hostAssembly)
    {
        ArgumentNullException.ThrowIfNull(hostAssembly);

        IConfigurationSection corsOrigins = configuration.GetSection("Cors:AllowedOrigins");
        int corsCount = corsOrigins.GetChildren().Count();

        int rateLimit = configuration.GetValue("RateLimiting:FixedWindow:PermitLimit", 0);

        BuildProvenance build = BuildProvenance.FromAssembly(hostAssembly);

        return new StartupConfigurationFacts(
            environment.EnvironmentName,
            environment.ContentRootPath,
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
            configuration.GetValue("SchemaValidation:EnableDetailedErrors", false),
            build.InformationalVersion,
            build.AssemblyVersion,
            build.FileVersion,
            build.CommitSha,
            build.RuntimeFrameworkDescription);
    }
}
