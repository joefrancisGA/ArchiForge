using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;

namespace ArchLucid.Cli.Commands;

/// <summary>
///     Downloads reference-evidence artifacts for a committed run (tenant scope) or a tenant-wide ZIP (admin).
/// </summary>
internal static class ReferenceEvidenceCommand
{
    private static readonly JsonSerializerOptions JsonReadOptions = new() { PropertyNameCaseInsensitive = true };

    public static async Task<int> RunAsync(string[] args, CancellationToken cancellationToken = default)
    {
        ReferenceEvidenceArgs parsed = ReferenceEvidenceArgs.Parse(args);

        if (!parsed.IsValid)
        {
            Console.WriteLine(
                "Usage: archlucid reference-evidence --run <runId> [--out <dir>] [--include-demo]\n"
                + "       archlucid reference-evidence --tenant <tenantId> [--out <dir>] [--include-demo]");

            return CliExitCode.UsageError;
        }

        ArchLucidProjectScaffolder.ArchLucidCliConfig? config = CliCommandShared.TryLoadConfigFromCwd();
        string baseUrl = CliCommandShared.GetBaseUrl(config);
        ApiConnectionOutcome outcome = await CliCommandShared.TryConnectToApiAsync(baseUrl, config, cancellationToken);

        if (outcome != ApiConnectionOutcome.Connected)
            return CliCommandShared.ExitCodeForFailedConnection(outcome);

        string normalized = baseUrl.Trim().TrimEnd('/');
        using HttpClient http = new();
        http.Timeout = TimeSpan.FromMinutes(3);
        http.BaseAddress = new Uri(normalized + "/");

        string? apiKey = Environment.GetEnvironmentVariable("ARCHLUCID_API_KEY");

        if (!string.IsNullOrWhiteSpace(apiKey))
        {
            http.DefaultRequestHeaders.Remove("X-Api-Key");
            http.DefaultRequestHeaders.Add("X-Api-Key", apiKey);
        }

        if (parsed.TenantId is { } tenantId)
            return await DownloadTenantZipAsync(http, tenantId, parsed.OutputDirectory, parsed.IncludeDemo,
                cancellationToken);

        return await DownloadRunBundleAsync(http, parsed.RunId!, parsed.OutputDirectory, parsed.IncludeDemo,
            cancellationToken);
    }

    private static async Task<int> DownloadTenantZipAsync(
        HttpClient http,
        Guid tenantId,
        string? outputDirectory,
        bool includeDemo,
        CancellationToken cancellationToken)
    {
        string query = includeDemo ? "?includeDemo=true" : string.Empty;

        using HttpResponseMessage response = await http.GetAsync(
            $"v1/admin/tenants/{tenantId:D}/reference-evidence{query}",
            cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            Console.WriteLine(
                "No committed run found for that tenant (or none after excluding demo runs). Use --include-demo to allow demo seed.");

            return CliExitCode.UsageError;
        }

        if (response.StatusCode is HttpStatusCode.Forbidden or HttpStatusCode.Unauthorized)
        {
            Console.WriteLine("Admin API key with AdminAuthority is required for --tenant exports.");

            return CliExitCode.OperationFailed;
        }

        if (!response.IsSuccessStatusCode)
        {
            string body = await response.Content.ReadAsStringAsync(cancellationToken);
            Console.WriteLine($"Error {(int)response.StatusCode}: {body}");

            return CliExitCode.OperationFailed;
        }

        byte[] zip = await response.Content.ReadAsByteArrayAsync(cancellationToken);
        string dir = outputDirectory ??
                     Path.Combine(Directory.GetCurrentDirectory(), "reference-evidence", $"tenant-{tenantId:D}");
        Directory.CreateDirectory(dir);
        string zipPath = Path.Combine(dir, $"reference-evidence-{tenantId:D}.zip");
        await File.WriteAllBytesAsync(zipPath, zip, cancellationToken);
        Console.WriteLine($"Wrote {zipPath}");

        return CliExitCode.Success;
    }

    private static async Task<int> DownloadRunBundleAsync(
        HttpClient http,
        string runId,
        string? outputDirectory,
        bool includeDemo,
        CancellationToken cancellationToken)
    {
        http.DefaultRequestHeaders.Accept.Clear();
        http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        using HttpResponseMessage deltasResponse =
            await http.GetAsync($"v1/pilots/runs/{Uri.EscapeDataString(runId)}/pilot-run-deltas", cancellationToken);

        if (deltasResponse.StatusCode == HttpStatusCode.NotFound)
        {
            Console.WriteLine($"Run '{runId}' was not found (or is out of scope).");

            return CliExitCode.UsageError;
        }

        if (!deltasResponse.IsSuccessStatusCode)
        {
            string body = await deltasResponse.Content.ReadAsStringAsync(cancellationToken);
            Console.WriteLine($"Error {(int)deltasResponse.StatusCode}: {body}");

            return CliExitCode.OperationFailed;
        }

        string deltasJson = await deltasResponse.Content.ReadAsStringAsync(cancellationToken);
        PilotRunDeltasCliShape? shape = JsonSerializer.Deserialize<PilotRunDeltasCliShape>(deltasJson, JsonReadOptions);

        if (shape?.IsDemoTenant == true && !includeDemo)
        {
            Console.WriteLine(
                "This run is the Contoso demo seed. Re-run with --include-demo only when you intentionally export demo numbers (never as a customer reference).");

            return CliExitCode.UsageError;
        }

        string dir = outputDirectory ?? Path.Combine(Directory.GetCurrentDirectory(), "reference-evidence", runId);
        Directory.CreateDirectory(dir);

        await File.WriteAllTextAsync(Path.Combine(dir, "pilot-run-deltas.json"), deltasJson, cancellationToken);

        http.DefaultRequestHeaders.Accept.Clear();
        http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("text/markdown"));

        using HttpResponseMessage mdResponse =
            await http.GetAsync($"v1/pilots/runs/{Uri.EscapeDataString(runId)}/first-value-report", cancellationToken);

        if (mdResponse.IsSuccessStatusCode)
        {
            string md = await mdResponse.Content.ReadAsStringAsync(cancellationToken);
            await File.WriteAllTextAsync(Path.Combine(dir, "first-value-report.md"), md, cancellationToken);
        }

        http.DefaultRequestHeaders.Accept.Clear();
        http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/pdf"));

        using HttpResponseMessage firstPdf =
            await http.PostAsync($"v1/pilots/runs/{Uri.EscapeDataString(runId)}/first-value-report.pdf", null,
                cancellationToken);

        if (firstPdf.IsSuccessStatusCode)
        {
            byte[] pdf = await firstPdf.Content.ReadAsByteArrayAsync(cancellationToken);
            await File.WriteAllBytesAsync(Path.Combine(dir, "first-value-report.pdf"), pdf, cancellationToken);
        }

        using HttpResponseMessage sponsorPdf =
            await http.PostAsync($"v1/pilots/runs/{Uri.EscapeDataString(runId)}/sponsor-one-pager", null,
                cancellationToken);

        if (sponsorPdf.IsSuccessStatusCode)
        {
            byte[] pdf = await sponsorPdf.Content.ReadAsByteArrayAsync(cancellationToken);
            await File.WriteAllBytesAsync(Path.Combine(dir, "sponsor-one-pager.pdf"), pdf, cancellationToken);
        }

        Console.WriteLine($"Wrote reference evidence under {dir}");

        return CliExitCode.Success;
    }

    private sealed class PilotRunDeltasCliShape
    {
        public bool IsDemoTenant
        {
            get;
            init;
        }
    }

    private sealed class ReferenceEvidenceArgs
    {
        public string? RunId
        {
            get;
            private init;
        }

        public Guid? TenantId
        {
            get;
            private init;
        }

        public string? OutputDirectory
        {
            get;
            private init;
        }

        public bool IncludeDemo
        {
            get;
            private init;
        }

        public bool IsValid => RunId is not null ^ TenantId is not null;

        public static ReferenceEvidenceArgs Parse(string[] args)
        {
            string? run = null;
            Guid? tenant = null;
            string? output = null;
            bool includeDemo = false;

            for (int i = 0; i < args.Length; i++)
            {
                string a = args[i];

                if (string.Equals(a, "--run", StringComparison.Ordinal) && i + 1 < args.Length)
                {
                    run = args[++i];

                    continue;
                }

                if (string.Equals(a, "--tenant", StringComparison.Ordinal) && i + 1 < args.Length
                                                                           && Guid.TryParse(args[++i], out Guid tid))
                {
                    tenant = tid;

                    continue;
                }

                if (string.Equals(a, "--out", StringComparison.Ordinal) && i + 1 < args.Length)
                {
                    output = args[++i];

                    continue;
                }

                if (string.Equals(a, "--include-demo", StringComparison.Ordinal))
                {
                    includeDemo = true;
                }
            }

            return new ReferenceEvidenceArgs
            {
                RunId = run,
                TenantId = tenant,
                OutputDirectory = output,
                IncludeDemo = includeDemo
            };
        }
    }
}
