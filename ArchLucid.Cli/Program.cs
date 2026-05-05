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
                    {
                        if (!TryParseNewCommandArgs(normalized.Skip(1).ToArray(), out string? projectName, out bool quickstart))
                        {
                            WriteNewUsage();

                            return CliExitCode.UsageError;
                        }

                        return await NewCommand.RunAsync(projectName!, quickstart);
                    }

                case "dev":
                    if (normalized.Length > 1 && normalized[1] == "up")
                        return await DevUpCommand.RunAsync();

                    Console.WriteLine("Expected: archlucid dev up");

                    return CliExitCode.UsageError;

                case "pilot":
                    if (normalized.Length > 1)
                    {
                        if (normalized[1] == "up")
                            return await PilotUpCommand.RunAsync();

                        if (normalized[1] == "success-criteria-template")
                            return await PilotSuccessCriteriaTemplateCommand.RunAsync();
                    }

                    Console.WriteLine("Expected: archlucid pilot up | archlucid pilot success-criteria-template");

                    return CliExitCode.UsageError;

                case "seed-demo-data":
                    return await SeedDemoDataCommand.RunAsync();

                case "explain-operator-model":
                    return await ExplainOperatorModelCommand.RunAsync();

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

                case "agent-eval":
                    if (normalized.Length > 1 && string.Equals(normalized[1], "rollup", StringComparison.OrdinalIgnoreCase))
                        return await AgentEvalRollupCommand.RunAsync(normalized.Skip(2).ToArray());

                    Console.WriteLine("Usage: archlucid agent-eval rollup --from-json <agent-evaluation.json> [--json]");

                    return CliExitCode.UsageError;

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

                case "manifest":
                    if (normalized.Length > 1 && string.Equals(normalized[1], "validate", StringComparison.Ordinal))
                        return await ManifestValidateCommand.RunAsync(
                            normalized
                                .Skip(2)
                                .ToArray());

                    if (CliExecutionContext.JsonOutput)
                        CliJson.WriteFailureLine(
                            Console.Error,
                            CliExitCode.UsageError,
                            "usage",
                            "Expected: archlucid manifest validate --file <path-to.json>");
                    else
                        Console.WriteLine("Usage: archlucid manifest validate --file <path-to.json>");

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
                case "proof-pack":
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

                case "validate-config":
                    return await ValidateConfigCommand.RunAsync(
                        normalized
                            .Skip(1)
                            .ToArray());

                case "webhooks":
                    if (normalized.Length > 1 && normalized[1] == "test")
                        return await WebhooksTestCommand.RunAsync(
                            normalized
                                .Skip(2)
                                .ToArray());

                    WebhooksTestCommand.WriteUsage(false);

                    return CliExitCode.UsageError;

                case "policy":
                    if (normalized.Length > 2 && normalized[1] == "validate")
                        return await PolicyValidateCommand.RunAsync(normalized[2]);

                    Console.WriteLine("Usage: archlucid policy validate <file.json>");

                    return CliExitCode.UsageError;

                case "graph":
                    if (normalized.Length > 2 && normalized[1] == "export")
                        return await GraphExportCommand.RunAsync(
                            normalized
                                .Skip(2)
                                .ToArray());

                    Console.WriteLine(
                        "Usage: archlucid graph export <runId> [--format mermaid] [--decision <key>] [--out <path>]");

                    return CliExitCode.UsageError;

                case "rules":
                    if (normalized.Length > 1 && normalized[1] == "simulate")
                        return await RulesSimulateCommand.RunAsync(
                            normalized
                                .Skip(2)
                                .ToArray());

                    Console.WriteLine(
                        "Usage: archlucid rules simulate --run <runGuid> [--severity Warning] [--count 3]");

                    return CliExitCode.UsageError;

                case "doctor":
                case "check":
                    return await DoctorCommand.RunAsync(CliCommandShared.TryLoadConfigFromCwd());

                case "support-bundle":
                    return await SupportBundleCommand.RunAsync(normalized.Skip(1).ToArray());

                case "completions":
                    return await CompletionsCommand.RunAsync(normalized.Skip(1).ToArray());

                case "config":
                    if (normalized.Length > 1 && string.Equals(normalized[1], "check", StringComparison.Ordinal))

                        return await ConfigCheckCommand.RunAsync(
                            normalized
                                .Skip(2)
                                .ToArray());

                    if (normalized.Length > 1 && string.Equals(normalized[1], "lint", StringComparison.Ordinal))

                        return await ConfigLintCommand.RunAsync(
                            normalized
                                .Skip(2)
                                .ToArray());

                    if (CliExecutionContext.JsonOutput)
                        CliJson.WriteFailureLine(
                            Console.Error,
                            CliExitCode.UsageError,
                            "usage",
                            "Expected: archlucid config check [--no-api] | archlucid config lint [--simulate-production] [--hosting-advisor]");
                    else
                        Console.WriteLine(
                            "Usage: archlucid config check [--no-api] · archlucid config lint [--simulate-production] [--hosting-advisor]");

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
        const string plain =
            "Please provide a command. Available commands: new [--quickstart], dev up, pilot up | pilot success-criteria-template, seed-demo-data, explain-operator-model, try [--api-base-url <url>] [--ui-base-url <url>] [--no-open] [--readiness-deadline <secs>] [--commit-deadline <secs>], second-run <SECOND_RUN.toml|json> [--api-base-url <url>] [--ui-base-url <url>] [--no-open] [--commit-deadline <secs>], trial smoke --org <name> --email <email> [--baseline-hours <n>] [--baseline-source <text>] [--api-base-url <url>] [--skip-pilot-run-deltas], roi-bulletin --quarter <Q-YYYY> [--min-tenants <n>] [--out <file.md>] [--synthetic] [--explain], security-trust publish --kind pen-test --date <YYYY-MM-DD> --summary-url <URL> [--assessor <name>] [--assessment-code <code>] [--ui-base-url <url>], marketplace preflight [--repo <dir>], manifest validate --file <path.json>, golden-cohort lock-baseline [--cohort <path>] [--write] | golden-cohort drift [--cohort <path>] [--strict-real] [--structural-only], run [--quick], status <runId>, trace <runId>, submit <runId> <result.json>, commit <runId>, seed <runId>, artifacts <runId>, first-value-report <runId> [--save], sponsor-one-pager <runId> [--save], reference-evidence | proof-pack (--run or --tenant; same CLI), comparisons list [filters], comparisons replay <comparisonRecordId> [--format <f>] [--mode <m>] [--profile <p>] [--persist], health, validate-config, policy validate <file.json>, graph export <runId> [--format mermaid] [--decision <key>] [--out <path>], rules simulate --run <runGuid> [--severity Warning] [--count 3], webhooks test [--url <url>] [--secret <s>] [--payload <path>] [--help], config check [--no-api], config lint [--simulate-production] [--hosting-advisor], doctor (or check), support-bundle [--output <dir>] [--zip], completions bash|zsh|powershell. Global: --json for machine-readable output where supported.";

        if (CliExecutionContext.JsonOutput)

            CliJson.WriteFailureLine(Console.Error, CliExitCode.UsageError, "usage", plain);

        else

            Console.WriteLine(plain);
    }

    private static void WriteNewUsage()
    {
        string plain =
            "Usage: archlucid new <projectName> [--quickstart]" + Environment.NewLine
                                                                + "  --quickstart  Provision local/quickstart artifacts: SQLite CLI registry (local/archlucid-evaluation.sqlite) "
                                                                + "and an appsettings fragment (local/archlucid.quickstart.appsettings.json) that sets "
                                                                + "ArchLucid:StorageProvider=InMemory so hosts run without SQL Server for initial evaluation.";

        if (CliExecutionContext.JsonOutput)

            CliJson.WriteFailureLine(Console.Error, CliExitCode.UsageError, "usage", plain);

        else

            Console.WriteLine(plain);
    }

    private static bool TryParseNewCommandArgs(
        string[] args,
        [NotNullWhen(true)] out string? projectName,
        out bool quickStartEvaluation)
    {
        projectName = null;
        quickStartEvaluation = false;

        if (args.Length == 0)
            return false;

        List<string> positionals = new();

        foreach (string arg in args)
        {
            if (string.Equals(arg, "--quickstart", StringComparison.OrdinalIgnoreCase))
            {
                quickStartEvaluation = true;

                continue;
            }

            if (arg.StartsWith('-'))
                return false;

            positionals.Add(arg);
        }

        if (positionals.Count != 1)
            return false;

        projectName = positionals[0];

        return true;
    }
}
