using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Text;
using System.Text.Json;

namespace ArchLucid.Cli.Commands;

/// <summary>
/// Draft aggregate ROI bulletin via <c>GET /v1/admin/roi-bulletin-preview</c> (AdminAuthority + API key).
/// Refuses when the tenant sample is below <c>--min-tenants</c> (HTTP 400 → <see cref="CliExitCode.UsageError"/>).
/// </summary>
[ExcludeFromCodeCoverage(Justification = "HTTP + file I/O; covered by RoiBulletinCommandTests with a mock HttpMessageHandler.")]
internal static class RoiBulletinCommand
{
    private static readonly JsonSerializerOptions JsonRead = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public static async Task<int> RunAsync(string[] args, CancellationToken cancellationToken = default)
    {
        RoiBulletinCommandOptions? opts = RoiBulletinCommandOptions.Parse(args, out string? parseError);

        if (opts is null)
        {
            Console.Error.WriteLine(parseError);
            Console.Error.WriteLine(
                "Usage: archlucid roi-bulletin --quarter <Q-YYYY> [--min-tenants <n>] [--out <file.md>]");

            return CliExitCode.UsageError;
        }

        ArchLucidProjectScaffolder.ArchLucidCliConfig? config = CliCommandShared.TryLoadConfigFromCwd();
        string baseUrl = CliCommandShared.GetBaseUrl(config);
        ApiConnectionOutcome outcome = await CliCommandShared.TryConnectToApiAsync(baseUrl, config, cancellationToken);

        if (outcome != ApiConnectionOutcome.Connected)
            return CliCommandShared.ExitCodeForFailedConnection(outcome);

        string normalized = baseUrl.Trim().TrimEnd('/');
        using HttpClient http = new() { BaseAddress = new Uri(normalized + "/") };
        http.DefaultRequestHeaders.Remove("Accept");
        http.DefaultRequestHeaders.Add("Accept", "application/json");

        string? apiKey = Environment.GetEnvironmentVariable("ARCHLUCID_API_KEY");

        if (!string.IsNullOrWhiteSpace(apiKey))
        {
            http.DefaultRequestHeaders.Remove("X-Api-Key");
            http.DefaultRequestHeaders.Add("X-Api-Key", apiKey);
        }

        string query =
            $"v1/admin/roi-bulletin-preview?quarter={Uri.EscapeDataString(opts.Quarter)}&minTenants={opts.MinTenants}";

        using HttpResponseMessage response = await http.GetAsync(query, cancellationToken);
        string responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

        if (response.StatusCode == HttpStatusCode.BadRequest)
        {
            await Console.Error.WriteLineAsync(
                "Below minimum tenant threshold or invalid quarter (API returned 400). " + Truncate(responseBody, 400));

            return CliExitCode.UsageError;
        }

        if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
        {
            await Console.Error.WriteLineAsync(
                "Admin API key with AdminAuthority is required for aggregate ROI bulletin preview.");

            return CliExitCode.OperationFailed;
        }

        if (!response.IsSuccessStatusCode)
        {
            await Console.Error.WriteLineAsync($"API error {(int)response.StatusCode}: {Truncate(responseBody, 400)}");

            return CliExitCode.OperationFailed;
        }

        RoiBulletinPreviewPayload? payload = JsonSerializer.Deserialize<RoiBulletinPreviewPayload>(responseBody, JsonRead);

        if (payload is null)
        {
            await Console.Error.WriteLineAsync("Empty or invalid JSON from roi-bulletin-preview.");

            return CliExitCode.OperationFailed;
        }

        string markdown = RoiBulletinMarkdownFormatter.FormatDraft(payload, opts.MinTenants);

        if (string.IsNullOrWhiteSpace(opts.OutPath))
        {
            Console.WriteLine(markdown);
            return CliExitCode.Success;
        }

        await File.WriteAllTextAsync(opts.OutPath, markdown, Encoding.UTF8, cancellationToken);
        Console.WriteLine($"Wrote draft bulletin to {opts.OutPath}");

        return CliExitCode.Success;
    }

    private static string Truncate(string s, int max) => s.Length <= max ? s : s[..max] + "…";
}
