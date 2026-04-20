using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text.Json;

namespace ArchLucid.Cli.Commands;

/// <summary>
/// Operator-facing readiness diagnostics: CLI build identity, local project layout,
/// API <c>GET /version</c>, and API <c>/health/live</c>, <c>/health/ready</c>, and optional combined <c>/health</c> (requires API key or JWT with read authority).
/// </summary>
[ExcludeFromCodeCoverage(Justification = "CLI doctor orchestrates HTTP probes via ArchLucidApiClient (excluded from coverage); exercised manually against a running API.")]
internal static class DoctorCommand
{
    private static readonly JsonSerializerOptions IndentedJson = new() { WriteIndented = true };

    public static async Task<int> RunAsync(ArchLucidProjectScaffolder.ArchLucidCliConfig? config, CancellationToken ct = default)
    {
        Console.WriteLine("ArchLucid doctor — local checks and API readiness");
        Console.WriteLine();

        PrintCliBuildInfo();
        RunLocalProjectChecks(config);
        PrintSaaSProfileHints();

        string baseUrl = ArchLucidApiClient.ResolveBaseUrl(config);
        string? urlError = ArchLucidApiClient.GetInvalidApiBaseUrlReason(baseUrl);

        if (urlError is not null)
        {
            await Console.Error.WriteLineAsync("[ArchLucid CLI] " + urlError);

            return CliExitCode.ConfigurationError;
        }

        Console.WriteLine("--- ArchLucid API ---");
        Console.WriteLine($"Base URL: {baseUrl}");

        ArchLucidApiClient client = new(baseUrl);

        await PrintApiVersionAsync(client, ct);

        await PrintProbeAsync(client, "/health/live", "Liveness (/health/live)", ct);
        bool readyOk = await PrintProbeAsync(client, "/health/ready", "Readiness (/health/ready)", ct);

        (int aggregateCode, string aggregateBody) = await client.GetHealthProbeAsync("/health", ct);
        Console.WriteLine();
        Console.WriteLine($"Detailed health (/health) HTTP {aggregateCode}");
        Console.WriteLine(TruncateForDisplay(aggregateBody, maxChars: 4000));

        if (!readyOk)
        {
            Console.WriteLine();
            Console.WriteLine("Readiness failed: fix the checks above before relying on this environment.");
            CliOperatorHints.WriteAfterReadinessFailed();

            return CliExitCode.OperationFailed;
        }

        if (aggregateCode == 401)
        {
            Console.WriteLine();
            Console.WriteLine(
                "Detailed /health requires authentication (ReadAuthority). Set X-Api-Key (e.g. ARCHLUCID_API_KEY) for full JSON. Liveness and readiness above are sufficient for a basic pass.");
        }
        else if (aggregateCode < 200 || aggregateCode >= 300)
        {
            Console.WriteLine();
            Console.WriteLine("Combined /health did not return 2xx; review JSON above.");
            CliOperatorHints.WriteAfterReadinessFailed();

            return CliExitCode.OperationFailed;
        }

        Console.WriteLine();
        Console.WriteLine(
            aggregateCode == 401
                ? "Doctor finished: readiness OK (detailed /health skipped — no credentials)."
                : "Doctor finished: readiness and detailed /health OK.");

        return CliExitCode.Success;
    }

    private static void PrintCliBuildInfo()
    {
        Console.WriteLine("--- CLI build info ---");

        Assembly cliAssembly = typeof(DoctorCommand).Assembly;
        AssemblyName name = cliAssembly.GetName();
        string assemblyVersion = name.Version?.ToString() ?? "unknown";

        string informational = cliAssembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
            ?? assemblyVersion;

        Console.WriteLine($"CLI version:    {informational}");
        Console.WriteLine($"Assembly:       {assemblyVersion}");
        Console.WriteLine($"Runtime:        {System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription}");
        Console.WriteLine();
    }

    private static async Task PrintApiVersionAsync(ArchLucidApiClient client, CancellationToken ct)
    {
        string? versionJson = await client.GetVersionJsonAsync(ct);

        if (versionJson is null)
        {
            Console.WriteLine();
            Console.WriteLine("API version: (unavailable — GET /version failed or not supported)");

            return;
        }

        Console.WriteLine();
        Console.WriteLine("API version (GET /version):");

        try
        {
            using JsonDocument doc = JsonDocument.Parse(versionJson);
            Console.WriteLine(JsonSerializer.Serialize(doc, IndentedJson));
        }
        catch (JsonException)
        {
            Console.WriteLine(versionJson);
        }
    }

    private static void RunLocalProjectChecks(ArchLucidProjectScaffolder.ArchLucidCliConfig? config)
    {
        Console.WriteLine("--- Local project ---");
        string cwd = Directory.GetCurrentDirectory();

        if (config is null)
        {
            Console.WriteLine(
                $"No archlucid.json in '{cwd}' (skipped local outputs/brief checks). API checks still run.");

            Console.WriteLine();

            return;
        }

        Console.WriteLine($"Project: {config.ProjectName} (schema {config.SchemaVersion})");

        string briefPath = Path.Combine(cwd, config.Inputs.Brief);
        Console.WriteLine(File.Exists(briefPath)
            ? $"Brief: OK — {config.Inputs.Brief}"
            : $"Brief: MISSING — expected file at {config.Inputs.Brief} (needed for 'archlucid run').");

        string outputsDir = Path.Combine(cwd, config.Outputs.LocalCacheDir);

        try
        {
            Directory.CreateDirectory(outputsDir);
            string probe = Path.Combine(outputsDir, ".archlucid-write-probe");
            File.WriteAllText(probe, "ok");
            File.Delete(probe);
            Console.WriteLine($"Outputs dir: OK — {config.Outputs.LocalCacheDir} is writable");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Outputs dir: FAIL — cannot use '{config.Outputs.LocalCacheDir}': {ex.Message}");
        }

        Console.WriteLine();
    }

    private static void PrintSaaSProfileHints()
    {
        Console.WriteLine("--- SaaS profile hints (operator checklist) ---");
        Console.WriteLine(
            "These rows are **not** fetched from the API host process; they inspect local environment variables " +
            "the SaaS profile expects. See `ArchLucid.Api/appsettings.SaaS.json` and `docs/FIRST_30_MINUTES.md`.");

        static string Cell(string value) => string.IsNullOrWhiteSpace(value) ? "MISSING" : "OK";

        string apiKey = Environment.GetEnvironmentVariable("ARCHLUCID_API_KEY") ?? string.Empty;
        string sql =
            Environment.GetEnvironmentVariable("ConnectionStrings__ArchLucid")
            ?? Environment.GetEnvironmentVariable("ARCHLUCID__ConnectionStrings__ArchLucid")
            ?? string.Empty;

        Console.WriteLine();
        Console.WriteLine("| Check | Status | How to fix |");
        Console.WriteLine("| --- | --- | --- |");
        Console.WriteLine($"| `ARCHLUCID_API_KEY` for `/health` aggregate | {Cell(apiKey)} | Export a read-capable API key (see `docs/runbooks/API_KEY_ROTATION.md`). |");
        Console.WriteLine($"| SQL connection string | {Cell(sql)} | Set `ConnectionStrings__ArchLucid` or `ARCHLUCID__ConnectionStrings__ArchLucid` (see `docs/FIRST_30_MINUTES.md`). |");
        Console.WriteLine("| `Authentication:ApiKey:DevelopmentBypassAll` | MANUAL | Must be **false** in SaaS; see `ArchLucid.Host.Core/Startup/AuthSafetyGuard.cs`. |");
        Console.WriteLine("| RLS bypass | MANUAL | `ArchLucid:Persistence:AllowRlsBypass` must stay **false** outside break-glass. |");
        Console.WriteLine();
    }

    private static async Task<bool> PrintProbeAsync(
        ArchLucidApiClient client,
        string path,
        string label,
        CancellationToken ct)
    {
        (int code, string body) = await client.GetHealthProbeAsync(path, ct);

        Console.WriteLine();
        Console.WriteLine($"{label} — HTTP {code}");
        Console.WriteLine(TruncateForDisplay(body, maxChars: 4000));

        return code is >= 200 and < 300;
    }

    private static string TruncateForDisplay(string body, int maxChars)
    {
        if (string.IsNullOrEmpty(body))
            return "(empty body)";


        if (body.Length <= maxChars)
            return body;


        return body[..maxChars] + "\n... (truncated)";
    }
}
