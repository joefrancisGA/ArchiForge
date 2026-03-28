using System.Collections.Generic;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace ArchiForge.Api.Tests;

public class ArchiForgeApiFactory : WebApplicationFactory<Program>
{
    private readonly string _connectionString;

    public ArchiForgeApiFactory()
    {
        string databaseName = "ArchiForgeTest_" + Guid.NewGuid().ToString("N");
        _connectionString =
            $"Server=localhost;Database={databaseName};Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=True";
        SqlServerTestDatabaseHelper.EnsureDatabaseExists(_connectionString);
    }

    /// <summary>
    /// Connection string for this factory’s SQL Server database (per-test database on <c>localhost</c>).
    /// Tests that open <see cref="Microsoft.Data.SqlClient.SqlConnection"/> must use this instance property so they hit the same DB as the hosted API.
    /// </summary>
    public string SqlConnectionString => _connectionString;

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
                // Best-effort cleanup (SQL Server may be unavailable on teardown).
            }
        }
    }
}
