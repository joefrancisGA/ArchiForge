using System.Net.Http.Headers;

namespace ArchLucid.Cli.Commands;

/// <summary>
/// Downloads the sponsor one-pager PDF from <c>POST /v1/pilots/runs/{runId}/sponsor-one-pager</c> (Standard tier).
/// </summary>
internal static class SponsorOnePagerCommand
{
    public static async Task<int> RunAsync(string runId, bool save, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(runId))
        {
            Console.WriteLine("Usage: archlucid sponsor-one-pager <runId> [--save]");

            return CliExitCode.UsageError;
        }

        ArchLucidProjectScaffolder.ArchLucidCliConfig? config = CliCommandShared.TryLoadConfigFromCwd();
        string baseUrl = CliCommandShared.GetBaseUrl(config);
        ApiConnectionOutcome outcome = await CliCommandShared.TryConnectToApiAsync(baseUrl, config, cancellationToken);

        if (outcome != ApiConnectionOutcome.Connected)
            return CliCommandShared.ExitCodeForFailedConnection(outcome);


        string normalized = baseUrl.Trim().TrimEnd('/');
        using HttpClient http = new() { Timeout = TimeSpan.FromSeconds(120) };
        http.BaseAddress = new Uri(normalized + "/");
        http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/pdf"));

        string? apiKey = Environment.GetEnvironmentVariable("ARCHLUCID_API_KEY");

        if (!string.IsNullOrWhiteSpace(apiKey))
        {
            http.DefaultRequestHeaders.Remove("X-Api-Key");
            http.DefaultRequestHeaders.Add("X-Api-Key", apiKey);
        }

        using HttpResponseMessage response = await http.PostAsync(
            $"v1/pilots/runs/{Uri.EscapeDataString(runId)}/sponsor-one-pager",
            content: null,
            cancellationToken);

        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            Console.WriteLine($"Run '{runId}' was not found (or is out of scope).");

            return CliExitCode.UsageError;
        }

        if (response.StatusCode == System.Net.HttpStatusCode.PaymentRequired)
        {
            Console.WriteLine("Tenant tier is below Standard (402 Payment Required).");

            return CliExitCode.OperationFailed;
        }

        if (!response.IsSuccessStatusCode)
        {
            string body = await response.Content.ReadAsStringAsync(cancellationToken);
            Console.WriteLine($"Error {(int)response.StatusCode}: {body}");

            return CliExitCode.OperationFailed;
        }

        byte[] pdf = await response.Content.ReadAsByteArrayAsync(cancellationToken);

        if (save)
        {
            string fileName = $"sponsor-one-pager-{runId}.pdf";
            string path = Path.Combine(Directory.GetCurrentDirectory(), fileName);
            await File.WriteAllBytesAsync(path, pdf, cancellationToken);
            Console.WriteLine($"Wrote {path}");
        }
        else
        {
            Console.WriteLine(Convert.ToBase64String(pdf));
        }

        return CliExitCode.Success;
    }
}
