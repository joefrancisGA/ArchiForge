using System.Collections.Generic;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace ArchiForge.Api.Tests;

/// <summary>
/// API host with <c>ArchiForge:StorageProvider=InMemory</c> so advisory scans use in-memory authority + alert stores (same DI graph as production, different backing stores).
/// </summary>
public sealed class AlertLifecycleWebAppFactory : WebApplicationFactory<Program>
{
    private readonly string _connectionString;

    public AlertLifecycleWebAppFactory()
    {
        string databaseName = "ArchiForgeAlertTest_" + Guid.NewGuid().ToString("N");
        _connectionString =
            $"Server=localhost;Database={databaseName};Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=True";
        SqlServerTestDatabaseHelper.EnsureDatabaseExists(_connectionString);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.UseSetting("ConnectionStrings:ArchiForge", _connectionString);
        builder.UseSetting("ArchiForge:StorageProvider", "InMemory");

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ArchiForge:StorageProvider"] = "InMemory",
                ["ConnectionStrings:ArchiForge"] = _connectionString
            });
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing)
        {
            try
            {
                SqlServerTestDatabaseHelper.DropDatabaseIfExists(_connectionString);
            }
            catch
            {
                // Best-effort cleanup.
            }
        }
    }
}
