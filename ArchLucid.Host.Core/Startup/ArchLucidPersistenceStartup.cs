using ArchLucid.Application.Bootstrap;
using ArchLucid.Core.Scoping;
using ArchLucid.Host.Core.Configuration;
using ArchLucid.Persistence.Data.Infrastructure;
using ArchLucid.Persistence.Sql;

namespace ArchLucid.Host.Core.Startup;

/// <summary>SQL DbUp migrations, idempotent consolidated bootstrap, and optional demo seed (shared by API and Worker).</summary>
public static class ArchLucidPersistenceStartup
{
    public static void RunSchemaBootstrapMigrationsAndOptionalDemoSeed(WebApplication app)
    {
        RlsBypassPolicyBootstrap.Apply(app.Configuration, app.Environment, app.Logger);

        ArchLucidOptions archLucidOptions = ArchLucidConfigurationBridge.ResolveArchLucidOptions(app.Configuration);
        bool storageIsSql = ArchLucidOptions.EffectiveIsSql(archLucidOptions.StorageProvider);

        // DbUp must run before SqlSchemaBootstrapper (ArchLucid.sql). On an empty database, the bootstrapper
        // creates objects that migration 001 also creates; DbUp then sees an empty journal and fails with
        // "already an object named …". Integration tests use DbUp-only on a fresh catalog; API startup should match.
        // After migrations, ArchLucid.sql is idempotent (IF OBJECT_ID …) and aligns greenfield with the reference DDL.
        if (storageIsSql)
        {
            string? connectionString = ArchLucidConfigurationBridge.ResolveSqlConnectionString(app.Configuration);

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                app.Logger.LogWarning(
                    "Startup: ConnectionStrings:ArchLucid is not set; skipping DbUp migrations.");
            }
            else
            {
                app.Logger.LogInformation(
                    "Startup: running DbUp migrations (embedded scripts under ArchLucid.Persistence/Migrations).");

                DatabaseMigrator.Run(connectionString);

                app.Logger.LogInformation("Startup: DbUp migrations completed successfully.");
            }
        }

        using (IServiceScope scope = app.Services.CreateScope())
        {
            if (storageIsSql)
            {
                app.Logger.LogInformation(
                    "Startup: running ISchemaBootstrapper (ArchLucid:StorageProvider=Sql).");

                ISchemaBootstrapper bootstrapper = scope.ServiceProvider.GetRequiredService<ISchemaBootstrapper>();
                using CancellationTokenSource cts = new(TimeSpan.FromSeconds(30));
                using (SqlRowLevelSecurityBypassAmbient.Enter())

                    bootstrapper.EnsureSchemaAsync(cts.Token).GetAwaiter().GetResult();

                app.Logger.LogInformation("Startup: schema bootstrap completed.");
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

        try
        {
            using IServiceScope seedScope = app.Services.CreateScope();
            IDemoSeedService demoSeed = seedScope.ServiceProvider.GetRequiredService<IDemoSeedService>();

            // SQL RLS predicates would otherwise block trusted startup inserts (same pattern as trial bootstrap).
            if (storageIsSql)
            {
                using (SqlRowLevelSecurityBypassAmbient.Enter())
                    demoSeed.SeedAsync(CancellationToken.None).GetAwaiter().GetResult();
            }
            else
            {
                demoSeed.SeedAsync(CancellationToken.None).GetAwaiter().GetResult();
            }

            app.Logger.LogInformation("Startup: demo seed completed.");
        }
        catch (Exception ex)
        {
            if (app.Logger.IsEnabled(LogLevel.Warning))
            {
                app.Logger.LogWarning(ex, "Startup: demo seed failed; continuing without demo data.");
            }
        }
    }
}
