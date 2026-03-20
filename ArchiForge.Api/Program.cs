using ArchiForge.Api.Configuration;
using ArchiForge.Api.Startup;
using ArchiForge.Data.Infrastructure;
using ArchiForge.Persistence.Sql;
using Serilog;

namespace ArchiForge.Api;

public partial class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Host.UseSerilog((context, services, configuration) => configuration
            .ReadFrom.Configuration(context.Configuration)
            .ReadFrom.Services(services)
            .Enrich.FromLogContext());

        // Add services to the container.

        builder.Services.AddArchiForgeMvc();

        builder.Services.AddArchiForgeAuth(builder.Configuration);
        builder.Services.AddArchiForgeAuthorization();

        builder.Services.AddArchiForgeOpenTelemetry(builder.Configuration, builder.Environment);
        builder.Services.AddArchiForgeRateLimiting(builder.Configuration);
        builder.Services.AddArchiForgeCors(builder.Configuration);
        builder.Services.AddArchiForgeApplicationServices(builder.Configuration);

        var app = builder.Build();

        using (var scope = app.Services.CreateScope())
        {
            var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
            var options = config.GetSection(ArchiForgeOptions.SectionName).Get<ArchiForgeOptions>();

            if (string.Equals(options?.StorageProvider, "Sql", StringComparison.OrdinalIgnoreCase))
            {
                var bootstrapper = scope.ServiceProvider.GetRequiredService<ISchemaBootstrapper>();
                bootstrapper.EnsureSchemaAsync(CancellationToken.None).GetAwaiter().GetResult();
            }
        }

        var connectionString = app.Configuration.GetConnectionString("ArchiForge");
        if (!string.IsNullOrEmpty(connectionString) && !DatabaseMigrator.Run(connectionString))
        {
            throw new InvalidOperationException("Database migration failed.");
        }

        app.UseArchiForgePipeline();
        app.Run();
    }
}

public partial class Program;