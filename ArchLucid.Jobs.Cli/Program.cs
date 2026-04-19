using ArchLucid.Core.Scoping;
using ArchLucid.Host.Composition.Startup;
using ArchLucid.Host.Core.Auth.Services;
using ArchLucid.Host.Core.Configuration;
using ArchLucid.Host.Core.Hosting;
using ArchLucid.Host.Core.Jobs;
using ArchLucid.Host.Core.Startup;
using ArchLucid.Host.Core.Startup.Diagnostics;
using ArchLucid.Host.Core.Startup.Validation;

namespace ArchLucid.Jobs.Cli;

/// <summary>One-shot job runner for Azure Container Apps Jobs (<c>dotnet ArchLucid.Jobs.Cli.dll --job advisory-scan</c>).</summary>
public static class Program
{
    /// <summary>Entry point.</summary>
    public static async Task<int> Main(string[] args) => await RunAsync(args);

    /// <summary>Used by tests and the console host.</summary>
    public static async Task<int> RunAsync(string[] args)
    {
        if (!JobsCommandLine.TryParseJobName(args, out string? jobName, out string? usageError))
        {
            await Console.Error.WriteLineAsync(usageError ?? "Invalid arguments.");

            return ArchLucidJobExitCodes.ConfigurationError;
        }

        WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

        builder.AddArchLucidGracefulShutdown();

        ArchLucidSerilogConfiguration.Configure(builder, "ArchLucid.Jobs.Cli");

        builder.Services.AddHttpContextAccessor();
        builder.Services.AddSingleton<IScopeContextProvider, HttpScopeContextProvider>();
        builder.Services.AddArchLucidOpenTelemetry(
            builder.Configuration,
            builder.Environment,
            telemetryServiceName: "ArchLucid.Jobs.Cli");
        builder.Services.AddArchLucidApplicationServices(builder.Configuration, ArchLucidHostingRole.Worker);

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

            return ArchLucidJobExitCodes.ConfigurationError;
        }

        StartupConfigurationDiagnostics.LogIfEnabled(
            app.Logger,
            app.Configuration,
            app.Environment,
            typeof(Program).Assembly);

        ArchLucidPersistenceStartup.RunSchemaBootstrapMigrationsAndOptionalDemoSeed(app);

        ArchLucidJobRunner runner = app.Services.GetRequiredService<ArchLucidJobRunner>();

        return await runner.RunNamedJobAsync(jobName!, CancellationToken.None);
    }
}
