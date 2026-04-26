using System.Globalization;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;

namespace ArchLucid.Cli.Support;

/// <summary>
///     Gathers explicit, reviewable sections for <see cref="SupportBundleArchiveWriter" />.
/// </summary>
public static class SupportBundleCollector
{
    /// <summary>Maximum characters stored per health response body to keep bundles compact.</summary>
    public const int MaxHealthBodyLength = 48_000;

    private static readonly JsonSerializerOptions JsonWrite = new() { WriteIndented = true };

    /// <summary>
    ///     Collects all sections. Uses <paramref name="client" /> for API probes; never logs or stores API keys.
    /// </summary>
    public static async Task<SupportBundlePayload> CollectAsync(
        ArchLucidApiClient client,
        string workingDirectory,
        ArchLucidProjectScaffolder.ArchLucidCliConfig? config,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(client);

        string createdUtc = DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture);
        string archLucidJsonPath = Path.Combine(workingDirectory, "archlucid.json");

        SupportBundleManifest manifest = new()
        {
            CreatedUtc = createdUtc,
            CliWorkingDirectory = workingDirectory,
            ArchLucidJsonPath = archLucidJsonPath,
            ArchLucidJsonPresent = File.Exists(archLucidJsonPath),
            TriageReadOrder = SupportBundleTriageCatalog.Entries
        };

        (string? versionJson, string? versionErr) = await TryGetVersionAsync(client, cancellationToken);

        SupportBundleBuildSection build = new()
        {
            Cli = ReadCliBuildInfo(),
            ApiVersionJson = versionJson,
            ApiVersionError = versionErr
        };

        SupportBundleHealthSection health = new()
        {
            Live = await ProbeAsync(client, "/health/live", cancellationToken),
            Ready = await ProbeAsync(client, "/health/ready", cancellationToken),
            Combined = await ProbeAsync(client, "/health", cancellationToken)
        };

        SupportBundleApiContractSection apiContract = await CollectApiContractSectionAsync(client, cancellationToken);

        SupportBundleConfigSummary configSummary = BuildConfigSummary(config);
        SupportBundleEnvironmentSection env = BuildEnvironmentSection();
        SupportBundleWorkspaceSection workspace = BuildWorkspaceSection(workingDirectory, config);
        SupportBundleReferencesSection references = BuildReferencesSection();
        SupportBundleLogsSection logs = new()
        {
            LocalLogExcerpt = TryReadSmallLocalLogExcerpt(workingDirectory, config)
        };

        return new SupportBundlePayload(
            manifest,
            build,
            health,
            apiContract,
            configSummary,
            env,
            workspace,
            references,
            logs);
    }

    private static async Task<SupportBundleApiContractSection> CollectApiContractSectionAsync(
        ArchLucidApiClient client,
        CancellationToken ct)
    {
        const string openApiPath = "/openapi/v1.json";
        const int maxCaptureBytes = 65_536;

        (int status, string preview, bool truncated) =
            await client.GetBoundedUtf8BodyAsync(openApiPath, maxCaptureBytes, ct);

        return new SupportBundleApiContractSection
        {
            MicrosoftOpenApiV1 = new SupportBundleBoundedHttpProbe
            {
                Path = openApiPath,
                HttpStatus = status,
                BodyPreview = preview,
                BodyTruncated = truncated,
                MaxBytesCaptured = maxCaptureBytes
            }
        };
    }

    private static SupportBundleCliBuildInfo ReadCliBuildInfo()
    {
        Assembly asm = typeof(SupportBundleCollector).Assembly;
        AssemblyName name = asm.GetName();

        string assemblyVersion = name.Version?.ToString() ?? "unknown";
        string informational = asm.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
                               ?? assemblyVersion;

        return new SupportBundleCliBuildInfo
        {
            InformationalVersion = informational,
            AssemblyVersion = assemblyVersion,
            RuntimeFramework = RuntimeInformation.FrameworkDescription
        };
    }

    private static async Task<(string? Json, string? Error)> TryGetVersionAsync(
        ArchLucidApiClient client,
        CancellationToken ct)
    {
        try
        {
            string? json = await client.GetVersionJsonAsync(ct);

            if (json is null)
                return (null, "GET /version returned non-success or empty body.");


            return (json, null);
        }
        catch (Exception ex)
        {
            return (null, ex.GetType().Name + ": " + ex.Message);
        }
    }

    private static async Task<SupportBundleHealthProbe> ProbeAsync(
        ArchLucidApiClient client,
        string path,
        CancellationToken ct)
    {
        (int code, string body) = await client.GetHealthProbeAsync(path, ct);

        bool truncated = body.Length > MaxHealthBodyLength;

        if (truncated)

            body = body[..MaxHealthBodyLength] + "\n... [truncated by ArchLucid support-bundle]";


        return new SupportBundleHealthProbe { Path = path, HttpStatus = code, Body = body, BodyTruncated = truncated };
    }

    private static SupportBundleConfigSummary BuildConfigSummary(
        ArchLucidProjectScaffolder.ArchLucidCliConfig? config)
    {
        if (config is null)
        {
            string fallbackUrl = SupportBundleRedactor.RedactHttpUrl(ArchLucidApiClient.ResolveBaseUrl(null));

            return new SupportBundleConfigSummary { HasArchlucidJson = false, ApiBaseUrlRedacted = fallbackUrl };
        }

        string resolved = ArchLucidApiClient.ResolveBaseUrl(config);

        return new SupportBundleConfigSummary
        {
            HasArchlucidJson = true,
            ProjectName = config.ProjectName,
            SchemaVersion = config.SchemaVersion,
            ApiBaseUrlRedacted = SupportBundleRedactor.RedactHttpUrl(resolved),
            InputsBriefPath = config.Inputs.Brief,
            OutputsLocalCacheDir = config.Outputs.LocalCacheDir,
            PluginsLockFile = config.Plugins?.LockFile,
            TerraformEnabled = config.Infra?.Terraform.Enabled,
            TerraformPath = config.Infra?.Terraform.Path,
            Architecture = config.Architecture
        };
    }

    private static SupportBundleEnvironmentSection BuildEnvironmentSection()
    {
        return new SupportBundleEnvironmentSection
        {
            MachineName = Environment.MachineName,
            OsDescription = RuntimeInformation.OSDescription,
            OsArchitecture = RuntimeInformation.OSArchitecture.ToString(),
            ProcessArchitecture = RuntimeInformation.ProcessArchitecture.ToString(),
            DotnetRuntime = RuntimeInformation.FrameworkDescription,
            TimeZone = TimeZoneInfo.Local.Id,
            ArchlucidAndDotnetEnvironment = SupportBundleRedactor.SnapshotEnvironmentForBundle()
        };
    }

    private static SupportBundleWorkspaceSection BuildWorkspaceSection(
        string workingDirectory,
        ArchLucidProjectScaffolder.ArchLucidCliConfig? config)
    {
        if (config is null)
            return new SupportBundleWorkspaceSection();


        string outputsDir = Path.Combine(workingDirectory, config.Outputs.LocalCacheDir);

        if (!Directory.Exists(outputsDir))

            return new SupportBundleWorkspaceSection { OutputsDirectory = outputsDir, OutputsExists = false };


        string[] files = Directory.GetFiles(outputsDir, "*", SearchOption.AllDirectories);
        long total = 0;

        foreach (string file in files)

            try
            {
                FileInfo info = new(file);
                total += info.Length;
            }
            catch (IOException)
            {
                // ignore unreadable files
            }
            catch (UnauthorizedAccessException)
            {
                // ignore
            }


        string[] top = Directory.GetFileSystemEntries(outputsDir);

        List<string> sample = top
            .Select(static p => Path.GetFileName(p))
            .OrderBy(static n => n, StringComparer.OrdinalIgnoreCase)
            .Take(40)
            .ToList();

        return new SupportBundleWorkspaceSection
        {
            OutputsDirectory = outputsDir,
            OutputsExists = true,
            FileCount = files.Length,
            TotalFileBytes = total,
            SampleTopLevelNames = sample
        };
    }

    private static SupportBundleReferencesSection BuildReferencesSection()
    {
        return new SupportBundleReferencesSection
        {
            ApiEndpoints =
            [
                "GET /version — build identity (no auth)",
                "GET /health/live — liveness",
                "GET /health/ready — readiness (summary JSON; no exception text)",
                "GET /health — combined checks (detailed JSON; requires ReadAuthority / API key when configured)",
                "GET /openapi/v1.json — Microsoft OpenAPI document (bounded capture in api-contract.json)"
            ],
            Documentation =
            [
                "Open README.txt in this folder first for triage order; manifest.json lists triageReadOrder as JSON.",
                "Correlate failures: response header X-Correlation-ID and problem JSON correlationId (API and operator proxy) match structured logs on the API host.",
                "docs/TROUBLESHOOTING.md",
                "docs/OPERATOR_QUICKSTART.md",
                "docs/CLI_USAGE.md"
            ]
        };
    }

    /// <summary>
    ///     Optional: first ~4 KiB of a small text file under outputs if present (never connection strings from other files).
    /// </summary>
    private static string? TryReadSmallLocalLogExcerpt(string workingDirectory,
        ArchLucidProjectScaffolder.ArchLucidCliConfig? config)
    {
        if (config is null)
            return null;


        string candidate = Path.Combine(workingDirectory, config.Outputs.LocalCacheDir, "last-run.log");

        if (!File.Exists(candidate))
            return null;


        try
        {
            FileInfo fi = new(candidate);

            if (fi.Length > 65_536)
                return "(file too large; omitted)";


            string text = File.ReadAllText(candidate, Encoding.UTF8);

            if (text.Length > 4_096)
                return text[..4_096] + "\n... [truncated]";


            return text;
        }
        catch (Exception)
        {
            return "(unreadable)";
        }
    }

    /// <summary>Serializes a section to indented JSON for writing to disk.</summary>
    public static string SerializeIndented<T>(T value)
    {
        return JsonSerializer.Serialize(value, JsonWrite);
    }
}
