using System.Diagnostics.CodeAnalysis;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

using ArchLucid.Contracts.Findings;
using ArchLucid.Contracts.Governance;

namespace ArchLucid.Cli.Commands;

/// <summary>
///     <c>archlucid rules simulate</c> — calls <c>POST /v1/governance/pre-commit/simulate</c>.
/// </summary>
[ExcludeFromCodeCoverage(
    Justification = "Governance HTTP probe; exercised via integration when API is wired.")]
internal static class RulesSimulateCommand
{
    private static readonly JsonSerializerOptions SimulateJson =
        new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
            PropertyNameCaseInsensitive = true
        };

    public static async Task<int> RunAsync(string[] args)
    {
        string? runId = null;
        ArchLucidProjectScaffolder.ArchLucidCliConfig? config = CliCommandShared.TryLoadConfigFromCwd();

        for (int i = 0; i < args.Length; i++)
        {
            string a = args[i];

            if (string.Equals(a, "--run", StringComparison.Ordinal) && i + 1 < args.Length)
            {
                runId = args[++i].Trim();
                continue;
            }

            if (string.Equals(a, "--severity", StringComparison.Ordinal) && i + 1 < args.Length)
            {
                if (!Enum.TryParse(args[++i].Trim(), true, out FindingSeverity _))
                {
                    await Console.Error.WriteLineAsync("Invalid --severity. Use Critical, Error, Warning, Info.");
                    return CliExitCode.UsageError;
                }

                continue;
            }

            if (string.Equals(a, "--count", StringComparison.Ordinal) && i + 1 < args.Length)
            {
                string rawCount = args[++i].Trim();

                if (!int.TryParse(rawCount, out int count) || count < 0 || count > 500)
                {
                    await Console.Error.WriteLineAsync("--count must be an integer in [0, 500].");
                    return CliExitCode.UsageError;
                }

                continue;
            }

            await Console.Error.WriteLineAsync(
                "Usage: archlucid rules simulate --run <runGuid> [--severity Warning] [--count 3]");
            return CliExitCode.UsageError;
        }

        if (string.IsNullOrWhiteSpace(runId))
        {
            await Console.Error.WriteLineAsync(
                "Usage: archlucid rules simulate --run <runGuid> [--severity Warning] [--count 3]");
            return CliExitCode.UsageError;
        }

        string baseUrl = CliCommandShared.GetBaseUrl(config);

        ApiConnectionOutcome connection = await CliCommandShared.TryConnectToApiAsync(baseUrl, config);

        if (connection != ApiConnectionOutcome.Connected)
            return CliCommandShared.ExitCodeForFailedConnection(connection);

        SimulateRequestBody body =
            new()
            {
            };

        Uri relativeUri = new("v1/governance/pre-commit/simulate", UriKind.Relative);

        using HttpClient http = ArchLucidApiClient.CreateSharedApiHttpClient(baseUrl, config);

        HttpResponseMessage response =
            await http.PostAsJsonAsync(relativeUri, body, SimulateJson);

        string respJson = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            await Console.Error.WriteLineAsync($"{(int)response.StatusCode} {response.StatusCode}: {TrimPreview(respJson, 560)}");
            return CliExitCode.OperationFailed;
        }

        PreCommitGateResult? result;

        try
        {
            result = JsonSerializer.Deserialize<PreCommitGateResult>(respJson, SimulateJson);
        }
        catch (JsonException)
        {
            await Console.Error.WriteLineAsync($"Could not parse gate result JSON ({TrimPreview(respJson, 400)}).");
            return CliExitCode.OperationFailed;
        }

        if (CliExecutionContext.JsonOutput)
            Console.WriteLine(respJson.Trim());
        else
            EmitHumanReadable(result);

        return CliExitCode.Success;
    }

    private static void EmitHumanReadable(PreCommitGateResult? result)
    {
        if (result is null)
        {
            Console.Error.WriteLine("Empty response body.");
            return;
        }

        Console.WriteLine(
            $"{(result.Blocked ? "BLOCKED" : "ALLOW")} — warnOnly={result.WarnOnly} policyPackId={result.PolicyPackId ?? "(none)"}");

        if (!string.IsNullOrWhiteSpace(result.Reason))
            Console.WriteLine($"Reason: {result.Reason}");

        Console.WriteLine($"Blocking finding ids ({result.BlockingFindingIds.Count}):");

        foreach (string id in result.BlockingFindingIds)
            Console.WriteLine($"  {id}");
    }

    private static string TrimPreview(string raw, int max)
    {
        raw = raw.ReplaceLineEndings(" ");

        return raw.Length <= max ? raw : raw[..max];
    }

    private sealed class SimulateRequestBody;
}
