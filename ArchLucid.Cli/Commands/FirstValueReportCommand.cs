using System.Net;
using System.Net.Http.Headers;

namespace ArchLucid.Cli.Commands;

/// <summary>
///     Downloads the sponsor Markdown report from <c>GET /v1/pilots/runs/{runId}/first-value-report</c>.
/// </summary>
internal static class FirstValueReportCommand
{
    public static async Task<int> RunAsync(string runId, bool save, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(runId))
        {
            Console.WriteLine("Usage: archlucid first-value-report <runId> [--save]");

            return CliExitCode.UsageError;
        }

        ArchLucidProjectScaffolder.ArchLucidCliConfig? config = CliCommandShared.TryLoadConfigFromCwd();
        string baseUrl = CliCommandShared.GetBaseUrl(config);
        ApiConnectionOutcome outcome = await CliCommandShared.TryConnectToApiAsync(baseUrl, config, cancellationToken);

        if (outcome != ApiConnectionOutcome.Connected)
            return CliCommandShared.ExitCodeForFailedConnection(outcome);

        string normalized = baseUrl.Trim().TrimEnd('/');
        using HttpClient http = new();
        http.Timeout = TimeSpan.FromSeconds(60);
        http.BaseAddress = new Uri(normalized + "/");
        http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("text/markdown"));

        string? apiKey = Environment.GetEnvironmentVariable("ARCHLUCID_API_KEY");

        if (!string.IsNullOrWhiteSpace(apiKey))
        {
            http.DefaultRequestHeaders.Remove("X-Api-Key");
            http.DefaultRequestHeaders.Add("X-Api-Key", apiKey);
        }

        using HttpResponseMessage response =
            await http.GetAsync($"v1/pilots/runs/{Uri.EscapeDataString(runId)}/first-value-report", cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            Console.WriteLine($"Run '{runId}' was not found (or is out of scope).");

            return CliExitCode.UsageError;
        }

        if (!response.IsSuccessStatusCode)
        {
            string body = await response.Content.ReadAsStringAsync(cancellationToken);
            Console.WriteLine($"Error {(int)response.StatusCode}: {body}");

            return CliExitCode.OperationFailed;
        }

        string markdown = await response.Content.ReadAsStringAsync(cancellationToken);

        if (save)
        {
            string fileName = $"first-value-{runId}.md";
            string path = Path.Combine(Directory.GetCurrentDirectory(), fileName);
            await File.WriteAllTextAsync(path, markdown, cancellationToken);
            Console.WriteLine($"Wrote {path}");
        }
        else
        {
            Console.WriteLine(markdown);
        }

        return CliExitCode.Success;
    }
}
