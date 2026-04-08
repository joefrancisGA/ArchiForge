using ArchLucid.Application.Bootstrap;
using ArchLucid.Core.Scoping;
using ArchLucid.Host.Core.Configuration;
using ArchLucid.Persistence.Data.Infrastructure;
using ArchLucid.Persistence.Sql;

namespace ArchLucid.Host.Core.Startup;

/// <summary>SQL schema bootstrap, DbUp migrations, and optional demo seed (shared by API and Worker).</summary>
public static class ArchLucidPersistenceStartup
{
    public static void RunSchemaBootstrapMigrationsAndOptionalDemoSeed(WebApplication app)
    {
        ArchLucidOptions archLucidOptions = ArchLucidConfigurationBridge.ResolveArchLucidOptions(app.Configuration);
        bool storageIsSql = string.Equals(archLucidOptions.StorageProvider, "Sql", StringComparison.OrdinalIgnoreCase);

        using (IServiceScope scope = app.Services.CreateScope())
        {

            if (storageIsSql)
            {
                app.Logger.LogInformation(
                    "Startup: running ISchemaBootstrapper (ArchLucid:StorageProvider=Sql or legacy ArchiForge:StorageProvider=Sql).");

                ISchemaBootstrapper bootstrapper = scope.ServiceProvider.GetRequiredService<ISchemaBootstrapper>();
                using CancellationTokenSource cts = new(TimeSpan.FromSeconds(30));
                using (SqlRowLevelSecurityBypassAmbient.Enter())

                    bootstrapper.EnsureSchemaAsync(cts.Token).GetAwaiter().GetResult();


                app.Logger.LogInformation("Startup: schema bootstrap completed.");
            }
        }

        // DbUp targets SQL only. Development often sets InMemory while base appsettings still carry a template SQL
        // connection string; running migrations would fail on Linux containers (ZAP CI, etc.).
        if (storageIsSql)
        {
            string? connectionString = ArchLucidConfigurationBridge.ResolveSqlConnectionString(app.Configuration);


            if (string.IsNullOrWhiteSpace(connectionString))
            {
                app.Logger.LogWarning(
                    "Startup: ConnectionStrings:ArchLucid (or legacy ConnectionStrings:ArchiForge) is not set; skipping DbUp migrations.");
            }
            else
            {
                app.Logger.LogInformation(
                    "Startup: running DbUp migrations (embedded scripts under ArchLucid.Persistence/Migrations).");

                DatabaseMigrator.Run(connectionString);

                app.Logger.LogInformation("Startup: DbUp migrations completed successfully.");
            }
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
