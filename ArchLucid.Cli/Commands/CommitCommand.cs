using System.Diagnostics.CodeAnalysis;

using ArchLucid.Cli;

namespace ArchLucid.Cli.Commands;

[ExcludeFromCodeCoverage(Justification = "CLI commit subcommand orchestrates HTTP via ArchLucidApiClient (excluded from coverage).")]
internal static class CommitCommand
{
    public static async Task<int> RunAsync(string runId)
    {
        string baseUrl = CliCommandShared.GetBaseUrl(CliCommandShared.TryLoadConfigFromCwd());

        ApiConnectionOutcome connection = await CliCommandShared.TryConnectToApiAsync(baseUrl);

        if (connection != ApiConnectionOutcome.Connected)
        {
            return CliCommandShared.ExitCodeForFailedConnection(connection);
        }

        ArchLucidApiClient client = new(baseUrl);

        ArchLucidApiClient.CommitRunResult? result = await client.CommitRunAsync(runId);

        if (result is null || !result.Success)
        {
            Console.WriteLine($"Error: {result?.Error ?? "Commit failed"}");
            CliOperatorHints.WriteAfterApiFailure(result?.HttpStatusCode, result?.Error);

            return CliExitCode.OperationFailed;
        }

        ArchLucidApiClient.CommitRunResponse resp = result.Response!;
        string version = resp.Manifest.Metadata.ManifestVersion;
        Console.WriteLine($"Committed: {resp.Manifest.SystemName}");
        Console.WriteLine($"Manifest version: {version}");

        if (resp.Warnings.Count > 0)
        {
            Console.WriteLine("Warnings:");

            foreach (string w in resp.Warnings)
            {
                Console.WriteLine($"  - {w}");
            }
        }

        Console.WriteLine();
        Console.WriteLine($"Use 'archlucid artifacts {runId}' to view the manifest.");

        return CliExitCode.Success;
    }
}
