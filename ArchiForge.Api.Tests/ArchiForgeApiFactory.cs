using System.Collections.Generic;

using ArchiForge.Data.Infrastructure;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace ArchiForge.Api.Tests;

public class ArchiForgeApiFactory : WebApplicationFactory<Program>
{
    private readonly string _sqliteConnectionString =
        $"Data Source=file:archiforge-test-{Guid.NewGuid():N}?mode=memory&cache=shared";

    /// <summary>
    /// Connection string for this factory’s in-memory SQLite (<see cref="IDbConnectionFactory"/>).
    /// Tests that open <see cref="Microsoft.Data.Sqlite.SqliteConnection"/> must use this instance property so they hit the same DB as the hosted API.
    /// </summary>
    public string SqliteConnectionString => _sqliteConnectionString;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        // Host-level settings merge into configuration early; helps when machine/CI env vars override appsettings.
        builder.UseSetting("ConnectionStrings:ArchiForge", _sqliteConnectionString);
        builder.UseSetting("ArchiForge:StorageProvider", "InMemory");

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ArchiForge:StorageProvider"] = "InMemory",
                ["ConnectionStrings:ArchiForge"] = _sqliteConnectionString
            });
        });

        // Runs after all app registrations: guarantees SQLite even if IConfiguration precedence or singleton timing is wrong.
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IDbConnectionFactory>();
            services.AddSingleton<IDbConnectionFactory>(new SqliteConnectionFactory(_sqliteConnectionString));
        });
    }
}
