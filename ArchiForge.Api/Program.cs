using System.Diagnostics.CodeAnalysis;

using ArchiForge.Api.Auth.Services;
using ArchiForge.Api.Configuration;
using ArchiForge.Host.Core.Configuration;
using ArchiForge.Api.Startup;
using ArchiForge.Host.Core.Auth.Services;
using ArchiForge.Host.Core.Hosting;
using ArchiForge.Host.Core.Startup;
using ArchiForge.Host.Core.Startup.Diagnostics;
using ArchiForge.Host.Core.Startup.Validation;
using ArchiForge.Application.Governance.Preview;
using ArchiForge.Core.Audit;
using ArchiForge.Core.Scoping;

namespace ArchiForge.Api;

[ExcludeFromCodeCoverage(Justification = "Application startup wiring; tested via integration tests against WebApplicationFactory.")]
public partial class Program
{
    public static void Main(string[] args)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

        ArchiForgeSerilogConfiguration.Configure(builder, "ArchiForge.Api");

        ArchiForgeHostingRole hostingRole = HostingRoleResolver.Resolve(builder.Configuration);

        // Add services to the container.

        builder.Services.AddArchiForgeMvc();

        builder.Services.AddHttpContextAccessor();
        // Singleton: stateless per-call resolution via IHttpContextAccessor; required so singleton IAgentCompletionClient (LLM cache) can resolve scope without capturing a scoped service from the root provider.
        builder.Services.AddSingleton<IScopeContextProvider, HttpScopeContextProvider>();
        builder.Services.AddScoped<IAuditService, AuditService>();

        builder.Services.AddArchiForgeAuth(builder.Configuration);
        builder.Services.AddArchiForgeAuthorization();

        builder.Services.AddArchiForgeOpenTelemetry(
            builder.Configuration,
            builder.Environment,
            telemetryServiceName: "ArchiForge.Api");
        builder.Services.AddArchiForgeRateLimiting(builder.Configuration);
        builder.Services.AddArchiForgeCors(builder.Configuration);
        builder.Services.AddArchiForgeResponseCompression();
        builder.Services.AddArchiForgeApplicationServices(builder.Configuration, hostingRole);
        builder.Services.AddArchiForgeApiWebLayerServices(builder.Configuration);
        builder.Services.AddScoped<IGovernancePreviewService, GovernancePreviewService>();

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
            typeof(Program).Assembly);

        ArchiForgePersistenceStartup.RunSchemaBootstrapMigrationsAndOptionalDemoSeed(app);

        app.Logger.LogInformation("ArchiForge API starting request pipeline.");
        app.UseArchiForgePipeline();
        app.Run();
    }
}

public partial class Program;
