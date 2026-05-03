using ArchLucid.Core.Integration;
using ArchLucid.Host.Core.Configuration;

namespace ArchLucid.Host.Core.Startup.Validation.Rules;

internal static class StorageRules
{
    public static void Collect(IConfiguration configuration, ArchLucidOptions archLucidOptions, List<string> errors)
    {
        bool storageIsSql = ArchLucidOptions.EffectiveIsSql(archLucidOptions.StorageProvider);

        if (!string.IsNullOrWhiteSpace(archLucidOptions.StorageProvider) &&
            !string.Equals(archLucidOptions.StorageProvider, "InMemory", StringComparison.OrdinalIgnoreCase) &&
            !storageIsSql)

            errors.Add(
                "ArchLucid:StorageProvider must be 'InMemory' or 'Sql' when set.");

        IntegrationEventsOptions integrationEvents =
            configuration.GetSection(IntegrationEventsOptions.SectionName).Get<IntegrationEventsOptions>()
            ?? new IntegrationEventsOptions();

        if (integrationEvents.TransactionalOutboxEnabled && !storageIsSql)

            errors.Add(
                "IntegrationEvents:TransactionalOutboxEnabled requires ArchLucid:StorageProvider Sql (transactional enqueue needs a shared SQL transaction).");

        string? connectionString = ArchLucidConfigurationBridge.ResolveSqlConnectionString(configuration);

        if (storageIsSql && string.IsNullOrWhiteSpace(connectionString))

            errors.Add(
                "ConnectionStrings:ArchLucid is required when ArchLucid:StorageProvider is Sql (or unset, defaulting to Sql).");
    }
}
