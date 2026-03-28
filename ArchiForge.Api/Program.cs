using ArchiForge.Api.Auth.Services;
using ArchiForge.Api.Configuration;
using ArchiForge.Api.Startup;
using ArchiForge.Application.Bootstrap;
using ArchiForge.Application.Governance.Preview;
using ArchiForge.Core.Audit;
using ArchiForge.Core.Scoping;
using ArchiForge.Data.Infrastructure;
using ArchiForge.Persistence.Sql;

using Serilog;

namespace ArchiForge.Api;

public partial class Program
{
    public static void Main(string[] args)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

        builder.Host.UseSerilog((context, services, configuration) => configuration
            .ReadFrom.Configuration(context.Configuration)
            .ReadFrom.Services(services)
            .Enrich.FromLogContext());

        // Add services to the container.

        builder.Services.AddArchiForgeMvc();

        builder.Services.AddHttpContextAccessor();
        builder.Services.AddScoped<IScopeContextProvider, HttpScopeContextProvider>();
        builder.Services.AddScoped<IAuditService, AuditService>();

        builder.Services.AddArchiForgeAuth(builder.Configuration);
        builder.Services.AddArchiForgeAuthorization();

        builder.Services.AddArchiForgeOpenTelemetry(builder.Configuration, builder.Environment);
        builder.Services.AddArchiForgeRateLimiting(builder.Configuration);
        builder.Services.AddArchiForgeCors(builder.Configuration);
        builder.Services.AddArchiForgeResponseCompression();
        builder.Services.AddArchiForgeApplicationServices(builder.Configuration);
        builder.Services.AddScoped<IGovernancePreviewService, GovernancePreviewService>();

        WebApplication app = builder.Build();

        app.Logger.LogInformation(
            "ArchiForge API host built. Environment={Environment}, ContentRoot={ContentRoot}",
            app.Environment.EnvironmentName,
            app.Environment.ContentRootPath);

        // 1) Optional: Persistence layer applies bundled ArchiForge.sql batches when StorageProvider=Sql (authority / extended tables).
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
                bootstrapper.EnsureSchemaAsync(cts.Token).GetAwaiter().GetResult();
                app.Logger.LogInformation("Startup: schema bootstrap completed.");
            }
        }

        // 2) DbUp: embedded ArchiForge.Data/Migrations/*.sql in deterministic lexicographic order.
        string? connectionString = app.Configuration.GetConnectionString("ArchiForge");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            app.Logger.LogWarning(
                "Startup: ConnectionStrings:ArchiForge is not set; skipping DbUp migrations.");
        }
        else
        {
            app.Logger.LogInformation(
                "Startup: running DbUp migrations (embedded scripts under ArchiForge.Data/Migrations).");

            if (!DatabaseMigrator.Run(connectionString))
            {
                throw new InvalidOperationException("Database migration failed; see DbUp console output.");
            }

            app.Logger.LogInformation("Startup: DbUp migrations completed successfully.");
        }

        // 3) Optional: deterministic demo dataset (Development + Demo:Enabled + Demo:SeedOnStartup only).
        if (app.Environment.IsDevelopment())
        {
            DemoOptions? demo = app.Configuration.GetSection(DemoOptions.SectionName).Get<DemoOptions>();
            if (demo is { Enabled: true, SeedOnStartup: true })
            {
                app.Logger.LogInformation(
                    "Startup: Demo:SeedOnStartup=true; running {Service}.",
                    nameof(IDemoSeedService));

                using IServiceScope seedScope = app.Services.CreateScope();
                IDemoSeedService demoSeed = seedScope.ServiceProvider.GetRequiredService<IDemoSeedService>();
                demoSeed.SeedAsync(CancellationToken.None).GetAwaiter().GetResult();
                app.Logger.LogInformation("Startup: demo seed completed.");
            }
        }

        app.Logger.LogInformation("ArchiForge API starting request pipeline.");
        app.UseArchiForgePipeline();
        app.Run();
    }
}

public partial class Program;
