using ArchiForge.Core.Audit;
using ArchiForge.Core.Scoping;
using ArchiForge.Host.Composition.Startup;
using ArchiForge.Host.Core.Auth.Services;
using ArchiForge.Host.Core.Hosting;
using ArchiForge.Host.Core.Startup;
using ArchiForge.Host.Core.Startup.Diagnostics;
using ArchiForge.Host.Core.Startup.Validation;

namespace ArchiForge.Worker;

/// <summary>Background worker host: advisory scans, data archival, retrieval indexing outbox (no public HTTP API).</summary>
public static class Program
{
    public static void Main(string[] args)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

        builder.AddArchiForgeGracefulShutdown();

        ArchiForgeSerilogConfiguration.Configure(builder, "ArchiForge.Worker");

        builder.Services.AddHttpContextAccessor();
        // Singleton: matches Api registration; LLM completion cache (singleton) resolves partition scope per call.
        builder.Services.AddSingleton<IScopeContextProvider, HttpScopeContextProvider>();
        builder.Services.AddScoped<IAuditService, AuditService>();

        builder.Services.AddArchiForgeOpenTelemetry(
            builder.Configuration,
            builder.Environment,
            telemetryServiceName: "ArchiForge.Worker");
        builder.Services.AddArchiForgeApplicationServices(builder.Configuration, ArchiForgeHostingRole.Worker);

        WebApplication app = builder.Build();

        IReadOnlyList<string> configurationErrors = ArchiForgeConfigurationRules.CollectErrors(
            app.Configuration,
            app.Environment);

        if (configurationErrors.Count > 0)
        {
            foreach (string error in configurationErrors)
            {
                app.Logger.LogError("Startup configuration error: {Error}", error);
            }

            throw new InvalidOperationException(
                "ArchiForge configuration is invalid. Fix the settings listed in the logs above, then restart.");
        }

        StartupConfigurationDiagnostics.LogIfEnabled(
            app.Logger,
            app.Configuration,
            app.Environment,
            typeof(Program).Assembly);

        ArchiForgePersistenceStartup.RunSchemaBootstrapMigrationsAndOptionalDemoSeed(app);

        app.Logger.LogInformation("ArchiForge Worker starting (hosted background services only).");
        app.UseArchiForgeWorkerPipeline();
        app.Run();
    }
}
