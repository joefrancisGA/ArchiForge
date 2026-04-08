using ArchLucid.Core.Audit;
using ArchLucid.Core.Scoping;
using ArchLucid.Host.Composition.Startup;
using ArchLucid.Host.Core.Auth.Services;
using ArchLucid.Host.Core.Hosting;
using ArchLucid.Host.Core.Startup;
using ArchLucid.Host.Core.Startup.Diagnostics;
using ArchLucid.Host.Core.Startup.Validation;

namespace ArchLucid.Worker;

/// <summary>Background worker host: advisory scans, data archival, retrieval indexing outbox (no public HTTP API).</summary>
public static class Program
{
    public static void Main(string[] args)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

        builder.AddArchLucidGracefulShutdown();

        ArchLucidSerilogConfiguration.Configure(builder, "ArchLucid.Worker");

        builder.Services.AddHttpContextAccessor();
        // Singleton: matches Api registration; LLM completion cache (singleton) resolves partition scope per call.
        builder.Services.AddSingleton<IScopeContextProvider, HttpScopeContextProvider>();
        builder.Services.AddScoped<IAuditService, AuditService>();

        builder.Services.AddArchLucidOpenTelemetry(
            builder.Configuration,
            builder.Environment,
            telemetryServiceName: "ArchLucid.Worker");
        builder.Services.AddArchLucidApplicationServices(builder.Configuration, ArchLucidHostingRole.Worker);

        WebApplication app = builder.Build();

        IReadOnlyList<string> configurationErrors = ArchLucidConfigurationRules.CollectErrors(
            app.Configuration,
            app.Environment);

        if (configurationErrors.Count > 0)
        {
            foreach (string error in configurationErrors)
            {
                app.Logger.LogError("Startup configuration error: {Error}", error);
            }

            throw new InvalidOperationException(
                "ArchLucid configuration is invalid. Fix the settings listed in the logs above, then restart.");
        }

        StartupConfigurationDiagnostics.LogIfEnabled(
            app.Logger,
            app.Configuration,
            app.Environment,
            typeof(Program).Assembly);

        ArchLucidPersistenceStartup.RunSchemaBootstrapMigrationsAndOptionalDemoSeed(app);

        app.Logger.LogInformation("ArchLucid Worker starting (hosted background services only).");
        app.UseArchLucidWorkerPipeline();
        app.Run();
    }
}
