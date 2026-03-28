using System.Collections.Generic;

using ArchiForge.Data.Infrastructure;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace ArchiForge.Api.Tests;

/// <summary>
/// API host with <c>ArchiForge:StorageProvider=InMemory</c> so advisory scans use in-memory authority + alert stores (same DI graph as production, different backing stores).
/// </summary>
public sealed class AlertLifecycleWebAppFactory : WebApplicationFactory<Program>
{
    private readonly string _sqliteConnectionString =
        $"Data Source=file:alert-lifecycle-{Guid.NewGuid():N}?mode=memory&cache=shared";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

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

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IDbConnectionFactory>();
            services.AddSingleton<IDbConnectionFactory>(new SqliteConnectionFactory(_sqliteConnectionString));
        });
    }
}
