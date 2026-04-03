using ArchiForge.Api.Auth.Services;
using ArchiForge.Api.Hosting;
using ArchiForge.Api.Startup;
using ArchiForge.Api.Startup.Diagnostics;
using ArchiForge.Api.Startup.Validation;
using ArchiForge.Core.Audit;
using ArchiForge.Core.Scoping;

namespace ArchiForge.Worker;

/// <summary>Background worker host: advisory scans, data archival, retrieval indexing outbox (no public HTTP API).</summary>
public static class Program
{
    public static void Main(string[] args)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

        ArchiForgeSerilogConfiguration.Configure(builder, "ArchiForge.Worker");

        builder.Services.AddHttpContextAccessor();
        builder.Services.AddScoped<IScopeContextProvider, HttpScopeContextProvider>();
        builder.Services.AddScoped<IAuditService, AuditService>();

        builder.Services.AddArchiForgeOpenTelemetry(builder.Configuration, builder.Environment);
        builder.Services.AddArchiForgeApplicationServices(builder.Configuration, ArchiForgeHostingRole.Worker);

        WebApplication app = builder.Build();

        IReadOnlyList<string> configurationErrors = ArchiForgeConfigurationRules.CollectErrors(
            app.Configuration,
            app.Environment);

        if (configurationErrors.Count > 0)
        {
            foreach (string error in configurationErrors)
            
                app.Logger.LogError("Startup configuration error: {Error}", error);
            

            throw new InvalidOperationException(
                "ArchiForge configuration is invalid. Fix the settings listed in the logs above, then restart.");
        }

        StartupConfigurationDiagnostics.LogIfEnabled(
            app.Logger,
            app.Configuration,
            app.Environment,
            typeof(ArchiForgeSerilogConfiguration).Assembly);

        ArchiForgePersistenceStartup.RunSchemaBootstrapMigrationsAndOptionalDemoSeed(app);

        app.Logger.LogInformation("ArchiForge Worker starting (hosted background services only).");
        app.UseArchiForgeWorkerPipeline();
        app.Run();
    }
}
