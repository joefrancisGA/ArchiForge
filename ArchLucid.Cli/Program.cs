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
            if (args.Length == 0)
            {
                Console.WriteLine(
                    "Please provide a command. Available commands: new, dev up, run [--quick], status <runId>, submit <runId> <result.json>, commit <runId>, seed <runId>, artifacts <runId>, comparisons list [filters], comparisons replay <comparisonRecordId> [--format <f>] [--mode <m>] [--profile <p>] [--persist], health, doctor (or check), support-bundle [--output <dir>] [--zip]");

                return 1;
            }

            string command = args[0];

            switch (command)
            {
                case "new":
                    if (args.Length > 1)
                        return await NewCommand.RunAsync(args[1]);

                    Console.WriteLine("Usage: archlucid new <projectName>");

                    return 1;

                case "dev":
                    if (args.Length > 1 && args[1] == "up")
                    {
                        return await DevUpCommand.RunAsync();
                    }

                    Console.WriteLine("Expected: archlucid dev up");

                    return 1;

                case "run":
                    bool quick = args.Length > 1 && args[1] == "--quick";

                    return await RunCommand.RunAsync(quick);

                case "status":
                    if (args.Length > 1)
                    {
                        return await StatusCommand.RunAsync(args[1]);
                    }

                    Console.WriteLine("Usage: archlucid status <runId>");

                    return 1;

                case "submit":
                    if (args.Length > 2)
                    {
                        return await SubmitCommand.RunAsync(args[1], args[2]);
                    }

                    Console.WriteLine("Usage: archlucid submit <runId> <result.json>");

                    return 1;

                case "commit":
                    if (args.Length > 1)
                    {
                        return await CommitCommand.RunAsync(args[1]);
                    }

                    Console.WriteLine("Usage: archlucid commit <runId>");

                    return 1;

                case "seed":
                    if (args.Length > 1)
                    {
                        return await SeedCommand.RunAsync(args[1]);
                    }

                    Console.WriteLine("Usage: archlucid seed <runId>");

                    return 1;

                case "artifacts":
                    if (args.Length <= 1)
                    {
                        Console.WriteLine("Usage: archlucid artifacts <runId> [--save]");

                        return 1;
                    }

                    bool saveArtifacts = args.Length > 2 && args[2] == "--save";

                    return await ArtifactsCommand.RunAsync(args[1], saveArtifacts);

                case "comparisons":
                    return await ComparisonsCommand.RunAsync(args.Skip(1).ToArray());

                case "health":
                    return await HealthCommand.RunAsync();

                case "doctor":
                case "check":
                    return await DoctorCommand.RunAsync(CliCommandShared.TryLoadConfigFromCwd());

                case "support-bundle":
                    return await SupportBundleCommand.RunAsync(args.Skip(1).ToArray());

                default:
                    Console.WriteLine($"Unknown command: {command}");

                    return 1;
            }
        }
    }
}
