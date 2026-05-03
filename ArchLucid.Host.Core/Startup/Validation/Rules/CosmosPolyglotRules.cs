using ArchLucid.Persistence.Cosmos;

namespace ArchLucid.Host.Core.Startup.Validation.Rules;

internal static class CosmosPolyglotRules
{
    public static void Collect(
        IConfiguration configuration,
        IWebHostEnvironment environment,
        List<string> errors)
    {
        CosmosDbOptions opts =
            configuration.GetSection(CosmosDbOptions.SectionName).Get<CosmosDbOptions>() ?? new CosmosDbOptions();

        if (!opts.AnyCosmosFeatureEnabled)
            return;

        if (string.IsNullOrWhiteSpace(opts.ConnectionString))
        {
            errors.Add(
                "CosmosDb:ConnectionString is required when any CosmosDb feature flag is enabled (GraphSnapshotsEnabled, AgentTracesEnabled, AuditEventsEnabled).");

            return;
        }

        if (environment.IsProduction() && ConnectionStringLooksLikeEmulator(opts.ConnectionString))

            errors.Add(
                "CosmosDb:ConnectionString must not target the Cosmos Emulator (localhost / 127.0.0.1) in Production.");
    }

    private static bool ConnectionStringLooksLikeEmulator(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            return false;

        string normalized = connectionString.Trim();

        return normalized.Contains("localhost:8081", StringComparison.OrdinalIgnoreCase)
               || normalized.Contains("127.0.0.1:8081", StringComparison.OrdinalIgnoreCase);
    }
}
