using System.Diagnostics.CodeAnalysis;

using ArchLucid.Cli;
using ArchLucid.Contracts.Common;

namespace ArchLucid.Cli.Commands;

[ExcludeFromCodeCoverage(Justification = "CLI status subcommand orchestrates HTTP via ArchLucidApiClient (excluded from coverage).")]
internal static class StatusCommand
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

        ArchLucidApiClient.GetRunResult? run = await client.GetRunAsync(runId);

        if (run is null)
        {
            Console.WriteLine($"Run '{runId}' not found. Ensure the ArchLucid API is running at {baseUrl}.");

            return CliExitCode.OperationFailed;
        }

        ArchLucidApiClient.RunInfo r = run.Run;
        ArchitectureRunStatus status = (ArchitectureRunStatus)r.Status;
        Console.WriteLine($"Run: {r.RunId}");
        Console.WriteLine($"Status: {status}");
        Console.WriteLine($"Created: {r.CreatedUtc:O}");

        if (r.CompletedUtc.HasValue)
        {
            Console.WriteLine($"Completed: {r.CompletedUtc:O}");
        }

        if (!string.IsNullOrEmpty(r.CurrentManifestVersion))
        {
            Console.WriteLine($"Manifest version: {r.CurrentManifestVersion}");
        }

        Console.WriteLine();
        Console.WriteLine("Tasks:");

        foreach (ArchLucidApiClient.AgentTaskInfo task in run.Tasks)
        {
            AgentType agentType = (AgentType)task.AgentType;
            AgentTaskStatus taskStatus = (AgentTaskStatus)task.Status;
            Console.WriteLine($"  {agentType}: {taskStatus} - {task.Objective}");
        }

        Console.WriteLine($"Results: {run.Results.Count} submitted");

        return CliExitCode.Success;
    }
}
