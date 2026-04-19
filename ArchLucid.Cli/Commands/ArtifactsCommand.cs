using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

namespace ArchLucid.Cli.Commands;

[ExcludeFromCodeCoverage(Justification = "CLI artifacts subcommand orchestrates HTTP via ArchLucidApiClient (excluded from coverage).")]
internal static class ArtifactsCommand
{
    public static async Task<int> RunAsync(string runId, bool saveArtifacts)
    {
        ArchLucidProjectScaffolder.ArchLucidCliConfig? config = CliCommandShared.TryLoadConfigFromCwd();
        string baseUrl = CliCommandShared.GetBaseUrl(config);

        ApiConnectionOutcome connection = await CliCommandShared.TryConnectToApiAsync(baseUrl);

        if (connection != ApiConnectionOutcome.Connected)
        {
            return CliCommandShared.ExitCodeForFailedConnection(connection);
        }

        ArchLucidApiClient client = new(baseUrl);

        ArchLucidApiClient.GetRunResult? run = await client.GetRunAsync(runId);

        if (run is null)
        {
            Console.WriteLine($"Run '{runId}' not found. Ensure the ArchLucid API is running at {baseUrl}.");
            CliOperatorHints.WriteAfterApiFailure(404, null);

            return CliExitCode.OperationFailed;
        }

        string? version = run.Run.CurrentManifestVersion;

        if (string.IsNullOrEmpty(version))
        {
            Console.WriteLine($"Run {runId} has not been committed. Submit all agent results and call commit first.");
            await Console.Error.WriteLineAsync(
                "Next: archlucid status <runId>, then submit results or use seed (Development), then archlucid commit.");

            return CliExitCode.OperationFailed;
        }

        object? manifest = await client.GetManifestAsync(version);

        if (manifest is null)
        {
            Console.WriteLine($"Manifest '{version}' not found.");
            CliOperatorHints.WriteAfterApiFailure(404, null);

            return CliExitCode.OperationFailed;
        }

        string json = JsonSerializer.Serialize(manifest, CliCommandShared.JsonWriteIndented);
        Console.WriteLine($"Manifest version: {version}");
        Console.WriteLine();

        if (saveArtifacts && config is not null)
        {
            try
            {
                string projectRoot = Directory.GetCurrentDirectory();
                string outputsDir = Path.Combine(projectRoot, config.Outputs.LocalCacheDir);
                Directory.CreateDirectory(outputsDir);
                string fileName = $"manifest-{version}.json";
                string filePath = Path.Combine(outputsDir, fileName);
                await File.WriteAllTextAsync(filePath, json);
                Console.WriteLine($"Saved to {filePath}");
                Console.WriteLine($"URI: {baseUrl}/v1/architecture/manifest/{version}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Could not save manifest to outputs: {ex.Message}");
                Console.WriteLine(json);
            }
        }
        else
        {
            Console.WriteLine(json);
        }

        return CliExitCode.Success;
    }
}
