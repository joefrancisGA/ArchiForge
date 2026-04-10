using System.Diagnostics.CodeAnalysis;

using ArchLucid.Api.Auth.Models;
using ArchLucid.Api.Auth.Services;
using ArchLucid.Api.Configuration;
using ArchLucid.Api.Startup;
using ArchLucid.Application.Governance.Preview;
using ArchLucid.Core.Audit;
using ArchLucid.Core.Scoping;
using ArchLucid.Host.Composition.Startup;
using ArchLucid.Host.Core.Configuration;
using ArchLucid.Host.Core.Auth.Services;
using ArchLucid.Host.Core.Hosting;
using ArchLucid.Host.Core.Startup;
using ArchLucid.Host.Core.Startup.Diagnostics;
using ArchLucid.Host.Core.Startup.Validation;

namespace ArchLucid.Api;

[ExcludeFromCodeCoverage(Justification = "Application startup wiring; tested via integration tests against WebApplicationFactory.")]
public partial class Program
{
    public static void Main(string[] args)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

        builder.AddArchLucidGracefulShutdown();

        ArchLucidSerilogConfiguration.Configure(builder, "ArchLucid.Api");

        ArchLucidHostingRole hostingRole = HostingRoleResolver.Resolve(builder.Configuration);

        // Add services to the container.

        builder.Services.AddArchLucidMvc();

        builder.Services.AddHttpContextAccessor();
        // Singleton: resolves scope from IHttpContextAccessor (or ambient overrides). IAgentCompletionClient is scoped; handlers receive per-request instances while this provider stays stateless.
        builder.Services.AddSingleton<IScopeContextProvider, HttpScopeContextProvider>();
        builder.Services.AddScoped<IAuditService, AuditService>();

        builder.Services.AddArchLucidAuth(builder.Configuration);
        builder.Services.AddArchLucidAuthorization();

        builder.Services.AddArchLucidOpenTelemetry(
            builder.Configuration,
            builder.Environment,
            telemetryServiceName: "ArchLucid.Api");
        builder.Services.AddArchLucidRateLimiting(builder.Configuration);
        builder.Services.AddArchLucidCors(builder.Configuration);
        builder.Services.AddArchLucidResponseCompression();
        builder.Services.AddArchLucidApplicationServices(builder.Configuration, hostingRole);
        builder.Services.AddArchLucidApiWebLayerServices(builder.Configuration);
        builder.Services.AddScoped<IGovernancePreviewService, GovernancePreviewService>();

        WebApplication app = builder.Build();

        ArchLucidLegacyConfigurationWarnings.LogIfLegacyKeysPresent(app.Configuration, app.Logger);

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

        // Belt-and-suspenders: refuse Production + DevelopmentBypass even if validation rules are bypassed later.
        ArchLucidAuthOptions authBound = ArchLucidAuthConfigurationBridge.Resolve(app.Configuration);

        if (app.Environment.IsProduction()
            && string.Equals(authBound.Mode, "DevelopmentBypass", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "ArchLucidAuth:Mode cannot be DevelopmentBypass when ASPNETCORE_ENVIRONMENT is Production.");
        }

        if (app.Environment.IsProduction()
            && app.Configuration.GetValue("Authentication:ApiKey:DevelopmentBypassAll", false))
        {
            throw new InvalidOperationException(
                "Authentication:ApiKey:DevelopmentBypassAll cannot be true when ASPNETCORE_ENVIRONMENT is Production.");
        }

        StartupConfigurationDiagnostics.LogIfEnabled(
            app.Logger,
            app.Configuration,
            app.Environment,
            typeof(Program).Assembly);

        ArchLucidPersistenceStartup.RunSchemaBootstrapMigrationsAndOptionalDemoSeed(app);

        app.Logger.LogInformation("ArchLucid API starting request pipeline.");
        app.UseArchLucidPipeline();
        app.Run();
    }
}

public partial class Program;
