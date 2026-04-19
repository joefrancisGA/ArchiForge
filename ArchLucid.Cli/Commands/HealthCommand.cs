using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

namespace ArchLucid.Cli.Commands;

[ExcludeFromCodeCoverage(Justification = "CLI health checks reachability via ArchLucidApiClient (excluded from coverage); smoke-tested via Program integration tests.")]
internal static class HealthCommand
{
    private static readonly JsonSerializerOptions JsonCamel = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public static async Task<int> RunAsync()
    {
        ArchLucidProjectScaffolder.ArchLucidCliConfig? config = CliCommandShared.TryLoadConfigFromCwd();
        string baseUrl = CliCommandShared.GetBaseUrl(config);
        ApiConnectionOutcome outcome = await CliCommandShared.TryConnectToApiAsync(baseUrl, config);

        if (outcome != ApiConnectionOutcome.Connected)
        {
            return CliCommandShared.ExitCodeForFailedConnection(outcome);
        }

        if (CliExecutionContext.JsonOutput)
        {
            object payload = new
            {
                ok = true,
                exitCode = CliExitCode.Success,
                baseUrl
            };
            Console.WriteLine(JsonSerializer.Serialize(payload, JsonCamel));
        }
        else
        {
            Console.WriteLine($"OK - ArchLucid API at {baseUrl} is reachable.");
        }

        return CliExitCode.Success;
    }
}
