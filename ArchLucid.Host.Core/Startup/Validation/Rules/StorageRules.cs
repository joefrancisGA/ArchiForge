using ArchLucid.Core.Integration;
using ArchLucid.Host.Core.Configuration;
using ArchLucid.Persistence.Connections;

namespace ArchLucid.Host.Core.Startup.Validation.Rules;

internal static class StorageRules
{
    public static void Collect(IConfiguration configuration, ArchLucidOptions archiForge, List<string> errors)
    {
        bool storageIsSql = string.Equals(archiForge.StorageProvider, "Sql", StringComparison.OrdinalIgnoreCase);

        if (!string.IsNullOrWhiteSpace(archiForge.StorageProvider) &&
            !string.Equals(archiForge.StorageProvider, "InMemory", StringComparison.OrdinalIgnoreCase) &&
            !storageIsSql)
        {
            errors.Add(
                "ArchLucid:StorageProvider (or legacy ArchiForge:StorageProvider) must be 'InMemory' or 'Sql' when set.");
        }

        IntegrationEventsOptions integrationEvents =
            configuration.GetSection(IntegrationEventsOptions.SectionName).Get<IntegrationEventsOptions>()
            ?? new IntegrationEventsOptions();

        if (integrationEvents.TransactionalOutboxEnabled && !storageIsSql)
        {
            errors.Add(
                "IntegrationEvents:TransactionalOutboxEnabled requires ArchLucid:StorageProvider Sql (or legacy ArchiForge:StorageProvider) (transactional enqueue needs a shared SQL transaction).");
        }

        string? connectionString = ArchLucidConfigurationBridge.ResolveSqlConnectionString(configuration);

        if (storageIsSql && string.IsNullOrWhiteSpace(connectionString))
        {
            errors.Add(
                "ConnectionStrings:ArchLucid (or legacy ConnectionStrings:ArchiForge) is required when ArchLucid:StorageProvider (or ArchiForge:StorageProvider) is Sql (or unset, defaulting to Sql).");
        }
    }
}
