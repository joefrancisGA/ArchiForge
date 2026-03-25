using ArchiForge.Data.Infrastructure;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace ArchiForge.Api.Tests;

public class ArchiForgeApiFactory : WebApplicationFactory<Program>
{
    /// <summary>Shared in-memory DB string; integration tests that touch SQLite directly must use the same value.</summary>
    public const string SqliteInMemoryConnectionString = "Data Source=file:archiforge-test?mode=memory&cache=shared";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("ConnectionStrings:ArchiForge", SqliteInMemoryConnectionString);
        builder.ConfigureServices(services =>
        {
            List<ServiceDescriptor> descriptors = services.Where(d => d.ServiceType == typeof(IDbConnectionFactory)).ToList();
            foreach (ServiceDescriptor d in descriptors)
                services.Remove(d);
            services.AddSingleton<IDbConnectionFactory>(
                new SqliteConnectionFactory(SqliteInMemoryConnectionString));
        });
    }
}
