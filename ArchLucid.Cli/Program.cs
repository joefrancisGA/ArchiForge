using System.Diagnostics.CodeAnalysis;

using ArchLucid.Cli.Commands;

namespace ArchLucid.Cli
{
    [ExcludeFromCodeCoverage(Justification = "CLI dispatch and console I/O; tested via CLI integration tests.")]
    public static class Program
    {
        private static async Task<int> Main(string[] args) => await RunAsync(args);

        /// <summary>
        /// Entry point for the CLI. Used by tests to assert exit codes and behavior.
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

                    case "first-value-report":
                        if (normalized.Length > 1)
                        {
                            bool saveReport = normalized.Skip(2).Contains("--save", StringComparer.Ordinal);

                            return await FirstValueReportCommand.RunAsync(normalized[1], saveReport);
                        }


                        Console.WriteLine("Usage: archlucid first-value-report <runId> [--save]");

                        return CliExitCode.UsageError;

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
                "Please provide a command. Available commands: new, dev up, pilot up, run [--quick], status <runId>, trace <runId>, submit <runId> <result.json>, commit <runId>, seed <runId>, artifacts <runId>, first-value-report <runId> [--save], comparisons list [filters], comparisons replay <comparisonRecordId> [--format <f>] [--mode <m>] [--profile <p>] [--persist], health, doctor (or check), support-bundle [--output <dir>] [--zip], completions bash|zsh|powershell";

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
}
