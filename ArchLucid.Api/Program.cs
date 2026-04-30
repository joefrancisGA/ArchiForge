using System.Diagnostics.CodeAnalysis;
using System.Net.Http.Headers;

using ArchLucid.Api.Hosting;

using ArchLucid.Core.Hosting;

using ArchLucid.Api.Auth.Models;
using ArchLucid.Api.Auth.Services;
using ArchLucid.Api.Configuration;
using ArchLucid.Api.Startup;
using ArchLucid.Application.Governance.Preview;
using ArchLucid.Core.Diagnostics;
using ArchLucid.Core.Scoping;
using ArchLucid.Host.Composition.Startup;
using ArchLucid.Host.Core.Auth.Services;
using ArchLucid.Host.Core.Configuration;
using ArchLucid.Host.Core.Hosting;
using ArchLucid.Host.Core.Startup;
using ArchLucid.Host.Core.Startup.Diagnostics;
using ArchLucid.Host.Core.Startup.Validation;

namespace ArchLucid.Api;

[ExcludeFromCodeCoverage(Justification =
    "Application startup wiring; tested via integration tests against WebApplicationFactory.")]
public partial class Program
{
    public static void Main(string[] args)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

        // Optional tuning (observability, replay batch limits, DOCX export profiles, integration hooks). Loaded after default JSON; see docs/PILOT_GUIDE.md.
        builder.Configuration.AddJsonFile(
            Path.Combine(builder.Environment.ContentRootPath, "appsettings.Advanced.json"),
            true,
            true);

        builder.Configuration.AddJsonFile(
            Path.Combine(builder.Environment.ContentRootPath, "appsettings.SaaS.json"),
            true,
            true);

        // Advanced.json is chained after default env vars, so it can override ARCHLUCID_* / ArchLucid__* (e.g.
        // ArchLucid:Persistence:AllowRlsBypass=false for fail-closed defaults). Re-apply environment variables so
        // deployment and CI break-glass (RLS bypass for DbUp + schema bootstrap) still wins.
        builder.Configuration.AddEnvironmentVariables();

        // DAST / defense in depth: omit Kestrel "Server" version token (ZAP 10036); TLS identity lives at the ingress.
        builder.WebHost.ConfigureKestrel(static options => options.AddServerHeader = false);

        builder.AddArchLucidGracefulShutdown();

        ArchLucidSerilogConfiguration.Configure(builder, "ArchLucid.Api");

        ArchLucidHostingRole hostingRole = HostingRoleResolver.Resolve(builder.Configuration);

        // Add services to the container.

        builder.Services.AddArchLucidMvc();

        using (ILoggerFactory authSafetyLoggerFactory = LoggerFactory.Create(logging =>
               {
                   logging.ClearProviders();
                   logging.AddConfiguration(builder.Configuration.GetSection("Logging"));
                   logging.AddConsole();
               }))
        {
            ILogger authSafetyLogger = authSafetyLoggerFactory.CreateLogger("AuthSafetyGuard");
            AuthSafetyGuard.GuardAllDevelopmentBypasses(builder.Configuration, builder.Environment, authSafetyLogger);
        }

        builder.Services.AddHttpContextAccessor();
        // Singleton: resolves scope from IHttpContextAccessor (or ambient overrides). IAgentCompletionClient is scoped; handlers receive per-request instances while this provider stays stateless.
        builder.Services.AddSingleton<IScopeContextProvider, HttpScopeContextProvider>();
        builder.Services.AddArchLucidAuth(builder.Configuration);
        builder.Services.AddArchLucidAuthorization();

        builder.Services.AddArchLucidOpenTelemetry(
            builder.Configuration,
            builder.Environment,
            "ArchLucid.Api");
        builder.Services.AddArchLucidRateLimiting(builder.Configuration);
        builder.Services.Configure<E2EHarnessOptions>(builder.Configuration.GetSection(E2EHarnessOptions.SectionName));
        builder.Services.AddArchLucidCors(builder.Configuration);
        builder.Services.AddArchLucidResponseCompression();
        builder.Services.AddArchLucidApplicationServices(builder.Configuration, hostingRole);
        builder.Services.AddArchLucidApiWebLayerServices(builder.Configuration);
        builder.Services.AddScoped<IGovernancePreviewService, GovernancePreviewService>();

        if (builder.Configuration.GetValue("Demo:Enabled", false))
        {
            builder.Services.AddHttpClient(
                TrialFunnelHealthProbe.HttpClientName,
                static client =>
                {
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                });
            builder.Services.AddHostedService<TrialFunnelHealthProbe>();
        }

        WebApplication app = builder.Build();

        ArchLucidLegacyConfigurationWarnings.LogIfLegacyKeysPresent(app.Configuration, app.Logger);

        // ADR 0030 PR A3 (2026-04-24): Coordinator:LegacyRunCommitPath was retired together with the
        // legacy ArchitectureRunCommitOrchestrator + RunCommitPathSelector + LegacyRunCommitPathOptions.
        // The authority-driven commit orchestrator is now the only IArchitectureRunCommitOrchestrator
        // implementation. If the configuration key is still present, it is now warned about by
        // ArchLucidLegacyConfigurationWarnings (legacy-keys path), not here.

        IReadOnlyList<string> configurationErrors = ArchLucidConfigurationRules.CollectErrors(
            app.Configuration,
            app.Environment);

        if (configurationErrors.Count > 0)
        {
            foreach (string error in configurationErrors)

                if (app.Logger.IsEnabled(LogLevel.Error))

                    app.Logger.LogError(
                        "Startup configuration error: {Error}",
                        LogSanitizer.Sanitize(error));


            throw new InvalidOperationException(
                "ArchLucid configuration is invalid. Fix the settings listed in the logs above, then restart.");
        }

        ArchLucidConfigurationRules.LogAgentExecutionRealModeInformation(app.Configuration, app.Logger);

        ProductionLikeHostingMisconfigurationAdvisor.LogWarningsIfPresent(
            app.Logger,
            app.Configuration,
            app.Environment);

        ArchLucidAuthOptions authBound = ArchLucidAuthConfigurationBridge.Resolve(app.Configuration);

        if (!app.Environment.IsProduction()
            && string.Equals(authBound.Mode, "JwtBearer", StringComparison.OrdinalIgnoreCase)
            && !string.IsNullOrWhiteSpace(authBound.JwtSigningPublicKeyPemPath.Trim()))

            if (app.Logger.IsEnabled(LogLevel.Warning))

                app.Logger.LogWarning(
                    "ArchLucidAuth:JwtSigningPublicKeyPemPath is set: JWTs are validated with a local RSA public key (CI / local E2E). Use Entra authority + metadata in real environments.");


        StartupConfigurationDiagnostics.LogIfEnabled(
            app.Logger,
            app.Configuration,
            app.Environment,
            typeof(Program).Assembly);

        ArchLucidPersistenceStartup.RunSchemaBootstrapMigrationsAndOptionalDemoSeed(app);

        if (app.Logger.IsEnabled(LogLevel.Information))

            app.Logger.LogInformation("ArchLucid API starting request pipeline.");

        app.UseArchLucidPipeline();
        app.Run();
    }
}

public partial class Program;
