using System.Diagnostics.CodeAnalysis;

namespace ArchLucid.Cli.Commands;

[ExcludeFromCodeCoverage(Justification = "CLI seed subcommand orchestrates HTTP via ArchLucidApiClient (excluded from coverage).")]
internal static class SeedCommand
{
    public static async Task<int> RunAsync(string runId)
    {
        string baseUrl = CliCommandShared.GetBaseUrl(CliCommandShared.TryLoadConfigFromCwd());

        if (!await CliCommandShared.EnsureApiConnectedAsync(baseUrl))
        {
            return 1;
        }

        ArchLucidApiClient client = new(baseUrl);

        ArchLucidApiClient.SeedFakeResultsResult? result = await client.SeedFakeResultsAsync(runId);

        if (result is null || !result.Success)
        {
            Console.WriteLine($"Error: {result?.Error ?? "Seed failed"}");
            Console.WriteLine("Note: seed-fake-results is only available when the API runs in Development.");
            CliOperatorHints.WriteAfterApiFailure(result?.HttpStatusCode, result?.Error);

            return 1;
        }

        Console.WriteLine($"Seeded {result.ResultCount} fake results for run {runId}");
        Console.WriteLine($"Use 'archlucid commit {runId}' to produce the manifest.");

        return 0;
    }
}
