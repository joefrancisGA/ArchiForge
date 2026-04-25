using ArchLucid.TestSupport;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace ArchLucid.Api.Tests;

/// <summary>
///     API host with <c>ArchLucid:StorageProvider=InMemory</c> so advisory scans use in-memory authority + alert stores
///     (same DI graph as production, different backing stores).
/// </summary>
public sealed class AlertLifecycleWebAppFactory : WebApplicationFactory<Program>
{
    private readonly string _connectionString;

    public AlertLifecycleWebAppFactory()
    {
        string databaseName = "ArchLucidAlertTest_" + Guid.NewGuid().ToString("N");
        _connectionString =
            SqlServerIntegrationTestConnections.CreateEphemeralApiDatabaseConnectionString(databaseName);
        SqlServerTestCatalogCommands.EnsureCatalogExists(_connectionString);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.UseSetting("ConnectionStrings:ArchLucid", _connectionString);
        builder.UseSetting("ArchLucid:StorageProvider", "InMemory");

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ArchLucid:StorageProvider"] = "InMemory", ["ConnectionStrings:ArchLucid"] = _connectionString
            });
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (!disposing)
            return;

        try
        {
            SqlServerTestCatalogCommands.DropCatalogIfExists(_connectionString);
        }
        catch
        {
            // Best-effort cleanup.
        }
    }
}
