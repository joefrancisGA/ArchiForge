using System.Diagnostics.CodeAnalysis;

namespace ArchLucid.Cli.Commands;

[ExcludeFromCodeCoverage(Justification = "CLI health checks reachability via ArchLucidApiClient (excluded from coverage); smoke-tested via Program integration tests.")]
internal static class HealthCommand
{
    public static async Task<int> RunAsync()
    {
        string baseUrl = CliCommandShared.GetBaseUrl(CliCommandShared.TryLoadConfigFromCwd());
        string? urlError = ArchLucidApiClient.GetInvalidApiBaseUrlReason(baseUrl);

        if (urlError is not null)
        {
            await Console.Error.WriteLineAsync("[ArchLucid CLI] " + urlError);

            return 1;
        }

        ArchLucidApiClient client = new(baseUrl);

        if (await client.CheckHealthAsync())
        {
            Console.WriteLine($"OK - ArchLucid API at {baseUrl} is reachable.");

            return 0;
        }

        Console.WriteLine($"FAIL - Cannot reach ArchLucid API at {baseUrl}");
        Console.WriteLine("Ensure the API is running: dotnet run --project ArchLucid.Api");

        return 1;
    }
}
