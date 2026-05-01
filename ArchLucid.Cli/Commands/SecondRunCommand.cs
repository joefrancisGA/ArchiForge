using System.Diagnostics;
using System.Net.Http.Headers;

using ArchLucid.Cli.SecondRun;
using ArchLucid.Contracts.Common;
using ArchLucid.Contracts.Requests;

namespace ArchLucid.Cli.Commands;

/// <summary>
///     <c>archlucid second-run</c> — adoption path from demo to a real committed run using a one-page TOML/JSON file.
/// </summary>
internal static class SecondRunCommand
{
    public static async Task<int> RunAsync(string[] args, CancellationToken cancellationToken = default)
    {
        SecondRunCommandOptions? options = SecondRunCommandOptions.Parse(args, out string? parseError);

        if (options is null)
        {
            await Console.Out.WriteLineAsync(parseError);

            return CliExitCode.UsageError;
        }

        SecondRunParseOutcome parsed = SecondRunInputParser.ParseFromFile(options.InputPath);

        if (!parsed.IsSuccess)
        {
            await Console.Out.WriteLineAsync(parsed.Message ?? "Invalid second-run file.");

            return MapParseFailureToExit(parsed.FailureCode);
        }

        ArchitectureRequest request = parsed.Request!;

        ArchLucidProjectScaffolder.ArchLucidCliConfig? config = CliCommandShared.TryLoadConfigFromCwd();
        string baseUrl = options.ApiBaseUrlFromArgument
            ? options.ApiBaseUrl.TrimEnd('/')
            : ArchLucidApiClient.ResolveBaseUrl(config);

        ApiConnectionOutcome connection =
            await CliCommandShared.TryConnectToApiAsync(baseUrl, config, cancellationToken);

        if (connection is not ApiConnectionOutcome.Connected)
            return CliCommandShared.ExitCodeForFailedConnection(connection);

        ArchLucidApiClient client = new(baseUrl, config);

        await Console.Out.WriteLineAsync("Step 1/4: POST /v1/architecture/request (your SECOND_RUN file)...");
        ArchLucidApiClient.CreateRunResult create = await client.CreateRunAsync(request, cancellationToken);

        if (!create.Success || create.Response is null)
        {
            await Console.Out.WriteLineAsync($"Error: {create.Error}");
            await SecondRunDiagnostics.WriteAsync(
                Console.Out,
                "create-run",
                create.StatusCode,
                create.CorrelationId,
                create.Error);

            return CliExitCode.OperationFailed;
        }

        string runId = create.Response.Run.RunId;
        await Console.Out.WriteLineAsync($"  RunId: {runId}");

        await Console.Out.WriteLineAsync();
        await Console.Out.WriteLineAsync("Step 2/4: POST /v1/architecture/run/{runId}/execute (best-effort)...");
        ArchLucidApiClient.ExecuteRunResult? exec = await client.ExecuteRunAsync(runId, cancellationToken);

        if (exec is null || !exec.Success)
            await Console.Out.WriteLineAsync(
                $"  Execute warning: {exec?.Error ?? "unknown"} — continuing with status poll.");

        await Console.Out.WriteLineAsync();
        await Console.Out.WriteLineAsync(
            $"Step 3/4: Poll until ReadyForCommit (deadline {options.CommitDeadline.TotalSeconds:n0}s)...");
        ArchitectureRunStatus reached = await TryCommand.PollForCommittableStatusAsync(
            async ct =>
            {
                ArchLucidApiClient.GetRunResult? detail = await client.GetRunAsync(runId, ct);

                if (detail is null)
                    return null;

                return detail.Run.Status;
            },
            options.CommitDeadline,
            options.PollInterval,
            cancellationToken);

        if (reached < ArchitectureRunStatus.ReadyForCommit)
        {
            await Console.Out.WriteLineAsync(
                $"  Run did not reach ReadyForCommit (observed {reached}). Trying seed-fake-results (Development hosts only)...");
            ArchLucidApiClient.SeedFakeResultsResult?
                seed = await client.SeedFakeResultsAsync(runId, ct: cancellationToken);

            if (seed is null || !seed.Success)
            {
                await Console.Out.WriteLineAsync($"Error: Seed fallback failed. {seed?.Error ?? "Unknown error."}");
                await SecondRunDiagnostics.WriteAsync(
                    Console.Out,
                    "seed-fake-results",
                    seed?.HttpStatusCode,
                    null,
                    seed?.Error);

                return CliExitCode.OperationFailed;
            }

            await Console.Out.WriteLineAsync($"  Seeded {seed.ResultCount} fake results.");
        }

        await Console.Out.WriteLineAsync();
        await Console.Out.WriteLineAsync("Step 4/4: POST /v1/architecture/run/{runId}/commit...");
        ArchLucidApiClient.CommitRunResult? commit = await client.CommitRunAsync(runId, cancellationToken);

        if (commit is null || !commit.Success)
        {
            await Console.Out.WriteLineAsync($"Error: Commit failed. {commit?.Error ?? "Unknown error."}");
            await SecondRunDiagnostics.WriteAsync(
                Console.Out,
                "commit",
                commit?.HttpStatusCode,
                commit?.CorrelationId,
                commit?.Error);

            return CliExitCode.OperationFailed;
        }

        string version = commit.Response?.Manifest.Metadata.ManifestVersion ?? "(unknown)";
        await Console.Out.WriteLineAsync($"  Committed. Manifest version: {version}");

        string reportPath = Path.Combine(Directory.GetCurrentDirectory(), $"first-value-{runId}.md");
        bool reportSaved = await DownloadFirstValueReportAsync(baseUrl, runId, reportPath, cancellationToken);

        if (!reportSaved)
        {
            await Console.Out.WriteLineAsync(
                "  (Warning: first-value report download did not succeed; the run committed but the Markdown was not saved.)");
        }
        else
        {
            await Console.Out.WriteLineAsync($"  Wrote {reportPath}");

            if (options.OpenArtifacts)
                TryOpenLocalFile(reportPath);
        }

        string firstValueUrl =
            $"{baseUrl}/v1/pilots/runs/{Uri.EscapeDataString(runId)}/first-value-report";
        string runUrl = $"{options.UiBaseUrl.TrimEnd('/')}/runs/{Uri.EscapeDataString(runId)}";

        await Console.Out.WriteLineAsync();
        await Console.Out.WriteLineAsync($"First-value report URL: {firstValueUrl}");
        await Console.Out.WriteLineAsync($"Operator UI: {runUrl}");

        if (options.OpenArtifacts)
            TryOpenUrl(runUrl);

        await Console.Out.WriteLineAsync();
        await Console.Out.WriteLineAsync("Done. Your inputs are now a committed manifest.");

        return CliExitCode.Success;
    }

    private static int MapParseFailureToExit(SecondRunParseFailureCode code)
    {
        return code switch
        {
            _ => CliExitCode.UsageError
        };
    }

    private static async Task<bool> DownloadFirstValueReportAsync(
        string apiBaseUrl,
        string runId,
        string savePath,
        CancellationToken cancellationToken)
    {
        try
        {
            using HttpClient http = new();
            http.Timeout = TimeSpan.FromSeconds(60);
            http.BaseAddress = new Uri(apiBaseUrl.TrimEnd('/') + "/");
            http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("text/markdown"));

            string? apiKey = Environment.GetEnvironmentVariable("ARCHLUCID_API_KEY");

            if (!string.IsNullOrWhiteSpace(apiKey))
                http.DefaultRequestHeaders.Add("X-Api-Key", apiKey);

            using HttpResponseMessage response = await http.GetAsync(
                $"v1/pilots/runs/{Uri.EscapeDataString(runId)}/first-value-report",
                cancellationToken);

            if (!response.IsSuccessStatusCode)
                return false;

            string markdown = await response.Content.ReadAsStringAsync(cancellationToken);
            await File.WriteAllTextAsync(savePath, markdown, cancellationToken);

            return true;
        }
        catch
        {
            return false;
        }
    }

    private static void TryOpenLocalFile(string path)
    {
        try
        {
            Process.Start(new ProcessStartInfo { FileName = path, UseShellExecute = true });
        }
        catch
        {
            // best-effort
        }
    }

    private static void TryOpenUrl(string url)
    {
        try
        {
            Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true });
        }
        catch
        {
            // best-effort
        }
    }
}
