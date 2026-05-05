using System.Diagnostics.CodeAnalysis;

using ArchLucid.Core.Hosting;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace ArchLucid.Cli.Commands;

/// <summary>
///     Production-configuration lint (<c>archlucid config lint</c>): auth misconfiguration traps by default;
///     optional hosted-advisor parity when <c>--hosting-advisor</c> is passed next to staged configuration.
/// </summary>
[ExcludeFromCodeCoverage(Justification = "Thin I/O facade; exercised via Cli integration tests.")]
internal static class ConfigLintCommand
{
    public static Task<int> RunAsync(string[] args, CancellationToken cancellationToken = default)
    {
        _ = cancellationToken;

        bool simulateProduction = args.Contains("--simulate-production", StringComparer.OrdinalIgnoreCase);

        bool hostingAdvisor = args.Contains("--hosting-advisor", StringComparer.OrdinalIgnoreCase);

        ArchLucidProjectScaffolder.ArchLucidCliConfig? cli = CliCommandShared.TryLoadConfigFromCwd();

        IConfiguration local = BuildMergedConfiguration(cli, simulateProduction);

        string envName =
            local["ASPNETCORE_ENVIRONMENT"]
            ?? Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
            ?? Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")
            ?? Environments.Production;

        string trimmedEnv = envName.Trim();

        List<string> errors = EvaluateAuthMisconfigurations(local, trimmedEnv);

        if (hostingAdvisor)
        {
            errors.AddRange(ProductionLikeHostingMisconfigurationAdvisor.DescribeWarningRecords(local, trimmedEnv)
                .Select(w => $"[HostingMisconfiguration:{w.RuleName}] {w.Message}"));
        }

        bool ok = errors.Count == 0;

        if (!ok)

            foreach (string line in errors)

                Console.Error.WriteLine(line);

        if (ok)

            Console.WriteLine(
                "config lint OK: no blocking findings (auth traps always; hosted advisor optional via --hosting-advisor).");

        return Task.FromResult(ok ? CliExitCode.Success : CliExitCode.OperationFailed);
    }


    private static List<string> EvaluateAuthMisconfigurations(IConfiguration cfg, string hostingEnvironmentName)
    {
        List<string> errors = [];

        bool isDevelopment = hostingEnvironmentName.Equals(Environments.Development, StringComparison.OrdinalIgnoreCase);

        string? archLucidEnv = cfg["ARCHLUCID_ENVIRONMENT"] ?? Environment.GetEnvironmentVariable("ARCHLUCID_ENVIRONMENT");

        bool envImpliesProductionLike =
            HostingEnvironmentNamePatterns.EnvironmentNameImpliesProductionLike(hostingEnvironmentName)
            || HostingEnvironmentNamePatterns.EnvironmentNameImpliesProductionLike(archLucidEnv ?? string.Empty);

        bool nonDevelopmentHosting = !isDevelopment || envImpliesProductionLike;

        string modeTrim =
            cfg["ArchLucidAuth:Mode"]?.Trim() ?? string.Empty;

        if (nonDevelopmentHosting
            && string.Equals(modeTrim, "DevelopmentBypass", StringComparison.OrdinalIgnoreCase))

            errors.Add(
                "ArchLucidAuth:Mode must not be DevelopmentBypass outside safe Development workstations (check ASPNETCORE_ENVIRONMENT / ARCHLUCID_ENVIRONMENT).");

        if (nonDevelopmentHosting && cfg.GetValue("Authentication:ApiKey:DevelopmentBypassAll", false))

            errors.Add(
                "Authentication:ApiKey:DevelopmentBypassAll must be false outside intentional Development workstations.");

        if (nonDevelopmentHosting && modeTrim.Length > 0)

        {
            bool jwt = string.Equals(modeTrim, "JwtBearer", StringComparison.OrdinalIgnoreCase);

            bool apiKey = string.Equals(modeTrim, "ApiKey", StringComparison.OrdinalIgnoreCase);

            if (!jwt && !apiKey)

                errors.Add("ArchLucidAuth:Mode must be JwtBearer or ApiKey when set for production-like hosting.");
        }

        return errors;
    }


    private static IConfiguration BuildMergedConfiguration(
        ArchLucidProjectScaffolder.ArchLucidCliConfig? cli,
        bool simulateProductionForLint)
    {
        List<KeyValuePair<string, string?>> overlays = [];

        if (cli is not null && !string.IsNullOrWhiteSpace(cli.ApiUrl))

            overlays.Add(
                new KeyValuePair<string, string?>("ARCHLUCID_API_URL", cli.ApiUrl.Trim().TrimEnd('/')));

        if (simulateProductionForLint)

            overlays.Add(new KeyValuePair<string, string?>("ASPNETCORE_ENVIRONMENT", Environments.Production));

        return new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("archlucid.json", true, true)
            .AddJsonFile("appsettings.json", true, true)
            .AddInMemoryCollection(overlays)
            .AddEnvironmentVariables()
            .Build();
    }
}
