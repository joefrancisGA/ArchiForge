using ArchiForge.Api.Configuration;
using ArchiForge.Application.Bootstrap;
using ArchiForge.Core.Scoping;
using ArchiForge.Data.Infrastructure;
using ArchiForge.Persistence.Sql;

namespace ArchiForge.Api.Startup;

/// <summary>SQL schema bootstrap, DbUp migrations, and optional demo seed (shared by API and Worker).</summary>
internal static class ArchiForgePersistenceStartup
{
    internal static void RunSchemaBootstrapMigrationsAndOptionalDemoSeed(WebApplication app)
    {
        using (IServiceScope scope = app.Services.CreateScope())
        {
            IConfiguration config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
            ArchiForgeOptions? options = config.GetSection(ArchiForgeOptions.SectionName).Get<ArchiForgeOptions>();

            if (string.Equals(options?.StorageProvider, "Sql", StringComparison.OrdinalIgnoreCase))
            {
                app.Logger.LogInformation(
                    "Startup: running ISchemaBootstrapper (ArchiForge:StorageProvider=Sql).");

                ISchemaBootstrapper bootstrapper = scope.ServiceProvider.GetRequiredService<ISchemaBootstrapper>();
                using CancellationTokenSource cts = new(TimeSpan.FromSeconds(30));
                using (SqlRowLevelSecurityBypassAmbient.Enter())
                
                    bootstrapper.EnsureSchemaAsync(cts.Token).GetAwaiter().GetResult();
                

                app.Logger.LogInformation("Startup: schema bootstrap completed.");
            }
        }

        string? connectionString = app.Configuration.GetConnectionString("ArchiForge");
        if (string.IsNullOrWhiteSpace(connectionString))
        
            app.Logger.LogWarning(
                "Startup: ConnectionStrings:ArchiForge is not set; skipping DbUp migrations.");
        
        else
        {
            app.Logger.LogInformation(
                "Startup: running DbUp migrations (embedded scripts under ArchiForge.Data/Migrations).");

            if (!DatabaseMigrator.Run(connectionString))
            
                throw new InvalidOperationException("Database migration failed; see DbUp console output.");
            

            app.Logger.LogInformation("Startup: DbUp migrations completed successfully.");
        }

        if (!app.Environment.IsDevelopment())
            return;

        DemoOptions? demo = app.Configuration.GetSection(DemoOptions.SectionName).Get<DemoOptions>();
        if (demo is not { Enabled: true, SeedOnStartup: true })
            return;

        app.Logger.LogInformation(
            "Startup: Demo:SeedOnStartup=true; running {Service}.",
            nameof(IDemoSeedService));

        using IServiceScope seedScope = app.Services.CreateScope();
        IDemoSeedService demoSeed = seedScope.ServiceProvider.GetRequiredService<IDemoSeedService>();
        demoSeed.SeedAsync(CancellationToken.None).GetAwaiter().GetResult();
        app.Logger.LogInformation("Startup: demo seed completed.");
    }
}
