using System.Diagnostics.CodeAnalysis;

using ArchLucid.Cli.Commands;

namespace ArchLucid.Cli;

[ExcludeFromCodeCoverage(Justification = "CLI dispatch and console I/O; tested via CLI integration tests.")]
public static class Program
{
    private static async Task<int> Main(string[] args)
    {
        return await RunAsync(args);
    }

    /// <summary>
    ///     Entry point for the CLI. Used by tests to assert exit codes and behavior.
    /// </summary>
    public static async Task<int> RunAsync(string[] args)
    {
        string[] normalized = CliExecutionContext.StripLeadingGlobalJsonFlags(args, out bool json);
        CliExecutionContext.JsonOutput = json;

        try
        {
            if (normalized.Length == 0)
            {
                WriteNoCommandMessage();

                return CliExitCode.UsageError;
            }

            string command = normalized[0];

            switch (command)
            {
                case "new":
                    if (normalized.Length > 1)
                        return await NewCommand.RunAsync(normalized[1]);

                    WriteNewUsage();

                    return CliExitCode.UsageError;

                case "dev":
                    if (normalized.Length > 1 && normalized[1] == "up")
                        return await DevUpCommand.RunAsync();


                    Console.WriteLine("Expected: archlucid dev up");

                    return CliExitCode.UsageError;

                case "pilot":
                    if (normalized.Length > 1 && normalized[1] == "up")
                        return await PilotUpCommand.RunAsync();


                    Console.WriteLine("Expected: archlucid pilot up");

                    return CliExitCode.UsageError;

                case "try":
                    return await TryCommand.RunAsync(normalized.Skip(1).ToArray());

                case "second-run":
                    return await SecondRunCommand.RunAsync(normalized.Skip(1).ToArray());

                case "trial":
                    if (normalized.Length > 1 && normalized[1] == "smoke")
                        return await TrialSmokeCommand.RunAsync(normalized.Skip(2).ToArray());

                    Console.WriteLine(
                        "Usage: archlucid trial smoke --org <name> --email <email> [--display-name <name>] " +
                        "[--baseline-hours <n>] [--baseline-source <text>] [--api-base-url <url>] [--skip-pilot-run-deltas]");

                    return CliExitCode.UsageError;

                case "roi-bulletin":
                    return await RoiBulletinCommand.RunAsync(normalized.Skip(1).ToArray());

                case "security-trust":
                    if (normalized.Length > 1 && normalized[1] == "publish")
                        return await SecurityTrustPublishCommand.RunAsync(normalized.Skip(2).ToArray());


                    Console.WriteLine(
                        "Usage: archlucid security-trust publish --kind pen-test --date <YYYY-MM-DD> "
                        + "--summary-url <URL> [--assessor <name>] [--assessment-code <code>] [--ui-base-url <url>]");

                    return CliExitCode.UsageError;

                case "marketplace":
                    if (normalized.Length > 1 && normalized[1] == "preflight")
                        return await MarketplacePreflightCommand.RunAsync(normalized.Skip(2).ToArray());


                    Console.WriteLine("Usage: archlucid marketplace preflight [--repo <dir>]");

                    return CliExitCode.UsageError;

                case "golden-cohort":
                    if (normalized.Length > 1 && normalized[1] == "lock-baseline")
                        return await GoldenCohortLockBaselineCommand.RunAsync(normalized.Skip(2).ToArray());

                    if (normalized.Length > 1 && normalized[1] == "drift")
                        return await GoldenCohortDriftCommand.RunAsync(normalized.Skip(2).ToArray());


                    Console.WriteLine(
                        "Usage: archlucid golden-cohort lock-baseline [--cohort <path>] [--write] | " +
                        "drift [--cohort <path>] [--strict-real] [--structural-only]");

                    return CliExitCode.UsageError;

                case "procurement-pack":
                    return await ProcurementPackCommand.RunAsync(normalized.Skip(1).ToArray());

                case "first-value-report":
                    if (normalized.Length > 1)
                    {
                        bool saveReport = normalized.Skip(2).Contains("--save", StringComparer.Ordinal);

                        return await FirstValueReportCommand.RunAsync(normalized[1], saveReport);
                    }


                    Console.WriteLine("Usage: archlucid first-value-report <runId> [--save]");

                    return CliExitCode.UsageError;

                case "sponsor-one-pager":
                    if (normalized.Length > 1)
                    {
                        bool savePdf = normalized.Skip(2).Contains("--save", StringComparer.Ordinal);

                        return await SponsorOnePagerCommand.RunAsync(normalized[1], savePdf);
                    }


                    Console.WriteLine("Usage: archlucid sponsor-one-pager <runId> [--save]");

                    return CliExitCode.UsageError;

                case "reference-evidence":
                    return await ReferenceEvidenceCommand.RunAsync(normalized.Skip(1).ToArray());

                case "run":
                    bool quick = normalized.Length > 1 && normalized[1] == "--quick";

                    return await RunCommand.RunAsync(quick);

                case "status":
                    if (normalized.Length > 1)
                        return await StatusCommand.RunAsync(normalized[1]);


                    Console.WriteLine("Usage: archlucid status <runId>");

                    return CliExitCode.UsageError;

                case "trace":
                    if (normalized.Length > 1)
                        return await TraceCommand.RunAsync(normalized[1]);


                    Console.WriteLine("Usage: archlucid trace <runId>");

                    return CliExitCode.UsageError;

                case "submit":
                    if (normalized.Length > 2)
                        return await SubmitCommand.RunAsync(normalized[1], normalized[2]);


                    Console.WriteLine("Usage: archlucid submit <runId> <result.json>");

                    return CliExitCode.UsageError;

                case "commit":
                    if (normalized.Length > 1)
                        return await CommitCommand.RunAsync(normalized[1]);


                    Console.WriteLine("Usage: archlucid commit <runId>");

                    return CliExitCode.UsageError;

                case "seed":
                    if (normalized.Length > 1)
                        return await SeedCommand.RunAsync(normalized[1]);


                    Console.WriteLine("Usage: archlucid seed <runId>");

                    return CliExitCode.UsageError;

                case "artifacts":
                    if (normalized.Length <= 1)
                    {
                        Console.WriteLine("Usage: archlucid artifacts <runId> [--save]");

                        return CliExitCode.UsageError;
                    }

                    bool saveArtifacts = normalized.Length > 2 && normalized[2] == "--save";

                    return await ArtifactsCommand.RunAsync(normalized[1], saveArtifacts);

                case "comparisons":
                    return await ComparisonsCommand.RunAsync(normalized.Skip(1).ToArray());

                case "health":
                    return await HealthCommand.RunAsync();

                case "doctor":
                case "check":
                    return await DoctorCommand.RunAsync(CliCommandShared.TryLoadConfigFromCwd());

                case "support-bundle":
                    return await SupportBundleCommand.RunAsync(normalized.Skip(1).ToArray());

                case "completions":
                    return await CompletionsCommand.RunAsync(normalized.Skip(1).ToArray());

                case "config":
                    if (normalized.Length > 1 && string.Equals(normalized[1], "check", StringComparison.Ordinal))
                    {
                        return await ConfigCheckCommand.RunAsync(
                            normalized
                                .Skip(2)
                                .ToArray());
                    }

                    if (CliExecutionContext.JsonOutput)
                    {
                        CliJson.WriteFailureLine(
                            Console.Error, CliExitCode.UsageError, "usage",
                            "Expected: archlucid config check [--no-api]");
                    }
                    else
                    {
                        Console.WriteLine("Usage: archlucid config check [--no-api]");
                    }

                    return CliExitCode.UsageError;

                default:
                    if (CliExecutionContext.JsonOutput)

                        CliJson.WriteFailureLine(
                            Console.Error,
                            CliExitCode.UsageError,
                            "unknown_command",
                            $"Unknown command: {command}");

                    else

                        Console.WriteLine($"Unknown command: {command}");


                    return CliExitCode.UsageError;
            }
        }
        finally
        {
            CliExecutionContext.JsonOutput = false;
        }
    }

    private static void WriteNoCommandMessage()
    {
        const string Plain =
            "Please provide a command. Available commands: new, dev up, pilot up, try [--api-base-url <url>] [--ui-base-url <url>] [--no-open] [--readiness-deadline <secs>] [--commit-deadline <secs>], second-run <SECOND_RUN.toml|json> [--api-base-url <url>] [--ui-base-url <url>] [--no-open] [--commit-deadline <secs>], trial smoke --org <name> --email <email> [--baseline-hours <n>] [--baseline-source <text>] [--api-base-url <url>] [--skip-pilot-run-deltas], roi-bulletin --quarter <Q-YYYY> [--min-tenants <n>] [--out <file.md>] [--synthetic] [--explain], security-trust publish --kind pen-test --date <YYYY-MM-DD> --summary-url <URL> [--assessor <name>] [--assessment-code <code>] [--ui-base-url <url>], marketplace preflight [--repo <dir>], golden-cohort lock-baseline [--cohort <path>] [--write] | golden-cohort drift [--cohort <path>] [--strict-real] [--structural-only], run [--quick], status <runId>, trace <runId>, submit <runId> <result.json>, commit <runId>, seed <runId>, artifacts <runId>, first-value-report <runId> [--save], sponsor-one-pager <runId> [--save], reference-evidence --run <runId> [--out <dir>] [--include-demo] | --tenant <tenantId> [--out <dir>] [--include-demo], comparisons list [filters], comparisons replay <comparisonRecordId> [--format <f>] [--mode <m>] [--profile <p>] [--persist], health, config check [--no-api], doctor (or check), support-bundle [--output <dir>] [--zip], completions bash|zsh|powershell. Global: --json for machine-readable output where supported.";

        if (CliExecutionContext.JsonOutput)

            CliJson.WriteFailureLine(Console.Error, CliExitCode.UsageError, "usage", Plain);

        else

            Console.WriteLine(Plain);
    }

    private static void WriteNewUsage()
    {
        const string Plain = "Usage: archlucid new <projectName>";

        if (CliExecutionContext.JsonOutput)

            CliJson.WriteFailureLine(Console.Error, CliExitCode.UsageError, "usage", Plain);

        else

            Console.WriteLine(Plain);
    }
}
