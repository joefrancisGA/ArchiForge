using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

using ArchLucid.Contracts.Agents;

namespace ArchLucid.Cli.Commands;

[ExcludeFromCodeCoverage(Justification = "CLI submit subcommand orchestrates HTTP via ArchLucidApiClient (excluded from coverage).")]
internal static class SubmitCommand
{
    public static async Task<int> RunAsync(string runId, string resultFilePath)
    {
        string baseUrl = CliCommandShared.GetBaseUrl(CliCommandShared.TryLoadConfigFromCwd());

        ApiConnectionOutcome connection = await CliCommandShared.TryConnectToApiAsync(baseUrl);

        if (connection != ApiConnectionOutcome.Connected)
        {
            return CliCommandShared.ExitCodeForFailedConnection(connection);
        }

        if (!File.Exists(resultFilePath))
        {
            Console.WriteLine($"Error: File not found: {resultFilePath}");

            return CliExitCode.OperationFailed;
        }

        AgentResult result;

        try
        {
            string json = await File.ReadAllTextAsync(resultFilePath);
            result = JsonSerializer.Deserialize<AgentResult>(json, CliCommandShared.JsonDeserializeAgentResult) ?? new AgentResult();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: Invalid result JSON. {ex.Message}");

            return CliExitCode.OperationFailed;
        }

        ArchLucidApiClient client = new(baseUrl);
        ArchLucidApiClient.SubmitResultResult? submitResult = await client.SubmitAgentResultAsync(runId, result);

        if (submitResult is null || !submitResult.Success)
        {
            Console.WriteLine($"Error: {submitResult?.Error ?? "Submit failed"}");
            CliOperatorHints.WriteAfterApiFailure(submitResult?.HttpStatusCode, submitResult?.Error);

            return CliExitCode.OperationFailed;
        }

        Console.WriteLine($"Result submitted: {submitResult.ResultId}");
        Console.WriteLine(
            $"Use 'archlucid status {runId}' to check progress, then 'archlucid commit {runId}' when all results are in.");

        return CliExitCode.Success;
    }
}
