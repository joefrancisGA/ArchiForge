using System.Globalization;
using System.IO.Compression;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using ArchLucid.Core.Support;
using Microsoft.Extensions.Options;

namespace ArchLucid.Application.Support;
/// <summary>
///     Default <see cref = "ISupportBundleAssembler"/>. Assembles a small, self-contained ZIP
///     from the running host's perspective (host environment, runtime info, version stamps,
///     redacted environment-variable snapshot, doc references) — suitable for attaching to
///     an inbound support ticket from <c>/admin/support</c>.
/// </summary>
/// <remarks>
///     <b>Why not call the CLI's <c>SupportBundleCollector</c>?</b> The CLI variant probes
///     <c>/health</c>, <c>/version</c>, and <c>/openapi/v1.json</c> over HTTP using
///     <c>ArchLucidApiClient</c>. Inside the host process we'd be calling ourselves over
///     the loopback adapter, doubling the HTTP plumbing for no signal gain. We instead
///     emit the host-side equivalents (build identity from <see cref = "Assembly"/>,
///     environment snapshot from <see cref = "SupportBundleSensitivePatternRedactor"/>,
///     and a static references section) and keep the file-name conventions identical so
///     a support engineer reading the ZIP cannot tell which side produced it.
///     <b>Redaction.</b> Every text section is filtered through
///     <see cref = "SupportBundleSensitivePatternRedactor.RedactSensitivePatterns"/> before
///     being written to the archive. The environment snapshot also masks values for
///     names matching the secret-shaped pattern list.
/// </remarks>
public sealed class SupportBundleAssembler(TimeProvider timeProvider, IOptionsMonitor<SupportBundleOptions> supportBundleOptions) : ISupportBundleAssembler
{
    private readonly byte __primaryConstructorArgumentValidation = __ValidatePrimaryConstructorArguments(timeProvider, supportBundleOptions);
    private static byte __ValidatePrimaryConstructorArguments(System.TimeProvider timeProvider, Microsoft.Extensions.Options.IOptionsMonitor<ArchLucid.Application.Support.SupportBundleOptions> supportBundleOptions)
    {
        ArgumentNullException.ThrowIfNull(timeProvider);
        ArgumentNullException.ThrowIfNull(supportBundleOptions);
        return (byte)0;
    }

    /// <summary>File names mirror the CLI <c>SupportBundleArchiveWriter</c> constants.</summary>
    public const string ReadmeFileName = "README.txt";
    /// <summary>Manifest file inside the ZIP.</summary>
    public const string ManifestFileName = "manifest.json";
    /// <summary>Build identity file inside the ZIP.</summary>
    public const string BuildFileName = "build.json";
    /// <summary>Environment snapshot file inside the ZIP.</summary>
    public const string EnvironmentFileName = "environment.json";
    /// <summary>Static references file inside the ZIP.</summary>
    public const string ReferencesFileName = "references.json";
    /// <summary>Bundle format version — bumped only on breaking changes to the file shape.</summary>
    public const string BundleFormatVersion = "server-1.1";
    /// <summary>Content type returned to the controller.</summary>
    public const string ZipContentType = "application/zip";
    private static readonly JsonSerializerOptions JsonWrite = new()
    {
        WriteIndented = true
    };
    private readonly IOptionsMonitor<SupportBundleOptions> _supportBundleOptions = supportBundleOptions ?? throw new ArgumentNullException(nameof(supportBundleOptions));
    private readonly TimeProvider _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    /// <inheritdoc/>
    public Task<SupportBundleArtifact> AssembleAsync(SupportBundleRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request is null)
            throw new ArgumentNullException(nameof(request));
        cancellationToken.ThrowIfCancellationRequested();
        DateTimeOffset generatedUtc = _timeProvider.GetUtcNow();
        string createdUtcIso = generatedUtc.UtcDateTime.ToString("O", CultureInfo.InvariantCulture);
        string requesterDisplay = string.IsNullOrWhiteSpace(request.RequesterDisplayId) ? "(unknown operator)" : request.RequesterDisplayId;
        string tenantDisplay = string.IsNullOrWhiteSpace(request.TenantDisplayName) ? "(no tenant context)" : request.TenantDisplayName;
        IReadOnlyDictionary<string, string> envSnapshot = SupportBundleSensitivePatternRedactor.SnapshotEnvironmentForBundle();
        SupportBundleNextStepsDocument nextSteps = SupportBundleNextStepsBuilder.BuildForApiHost(createdUtcIso, envSnapshot);
        string manifestJson = SerializeIndented(BuildManifest(createdUtcIso, requesterDisplay, tenantDisplay));
        string buildJson = SerializeIndented(BuildBuildSection());
        string environmentJson = SerializeIndented(BuildEnvironmentSection(envSnapshot));
        string referencesJson = SerializeIndented(BuildReferencesSection());
        string nextStepsJson = SerializeIndented(nextSteps);
        string readmeText = BuildReadme(createdUtcIso, requesterDisplay, tenantDisplay, nextSteps);
        byte[] zipBytes = WriteZip([new SupportBundleZipEntry(ReadmeFileName, RedactToBytes(readmeText)), new SupportBundleZipEntry(SupportBundleLayout.NextStepsFileName, RedactToBytes(nextStepsJson)), new SupportBundleZipEntry(ManifestFileName, RedactToBytes(manifestJson)), new SupportBundleZipEntry(BuildFileName, RedactToBytes(buildJson)), new SupportBundleZipEntry(EnvironmentFileName, RedactToBytes(environmentJson)), new SupportBundleZipEntry(ReferencesFileName, RedactToBytes(referencesJson))]);
        string fileName = "archlucid-support-bundle-" + generatedUtc.UtcDateTime.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture) + "Z.zip";
        int retentionDays = Math.Max(1, _supportBundleOptions.CurrentValue.BundleRetentionDays);
        DateTimeOffset retentionDiscardAfterUtc = generatedUtc.AddDays(retentionDays);
        return Task.FromResult(new SupportBundleArtifact(zipBytes, fileName, ZipContentType, generatedUtc, retentionDiscardAfterUtc));
    }

    private static byte[] RedactToBytes(string text)
    {
        return Encoding.UTF8.GetBytes(SupportBundleSensitivePatternRedactor.RedactSensitivePatterns(text));
    }

    private static object BuildManifest(string createdUtcIso, string requesterDisplay, string tenantDisplay)
    {
        return new
        {
            bundleFormatVersion = BundleFormatVersion,
            source = "api",
            createdUtc = createdUtcIso,
            requesterDisplayId = requesterDisplay,
            tenantDisplayName = tenantDisplay,
            triageReadOrder = new object[]
            {
                new
                {
                    file = ReadmeFileName,
                    why = "Plain-text overview — open first."
                },
                new
                {
                    file = SupportBundleLayout.NextStepsFileName,
                    why = "Machine-generated triage summary (advisory only)."
                },
                new
                {
                    file = ManifestFileName,
                    why = "Bundle metadata + read order in machine-readable form."
                },
                new
                {
                    file = BuildFileName,
                    why = "Host build identity (assembly version, runtime)."
                },
                new
                {
                    file = EnvironmentFileName,
                    why = "Redacted host environment snapshot."
                },
                new
                {
                    file = ReferencesFileName,
                    why = "Doc links and correlation hints."
                }
            },
            notes = "Server-assembled bundle. Sensitive env-var values appear only as (set)/(not set); " + "bearer tokens, X-Api-Key headers, and connection-string passwords are replaced with [REDACTED]."
        };
    }

    private static object BuildBuildSection()
    {
        Assembly asm = typeof(SupportBundleAssembler).Assembly;
        AssemblyName name = asm.GetName();
        string assemblyVersion = name.Version?.ToString() ?? "unknown";
        string informational = asm.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? assemblyVersion;
        return new
        {
            informationalVersion = informational,
            assemblyVersion,
            runtimeFramework = RuntimeInformation.FrameworkDescription,
            machineName = Environment.MachineName,
            osDescription = RuntimeInformation.OSDescription,
            osArchitecture = RuntimeInformation.OSArchitecture.ToString(),
            processArchitecture = RuntimeInformation.ProcessArchitecture.ToString(),
            timeZoneId = TimeZoneInfo.Local.Id
        };
    }

    private static object BuildEnvironmentSection(IReadOnlyDictionary<string, string> archlucidAndDotnetEnvironment)
    {
        return new
        {
            archlucidAndDotnetEnvironment,
            notes = "Only ARCHLUCID_* and DOTNET_* variables are included. Secret-shaped names show (set)/(not set) only."
        };
    }

    private static object BuildReferencesSection()
    {
        return new
        {
            apiEndpoints = new[]
            {
                "GET /version — build identity (no auth)",
                "GET /health/live — liveness",
                "GET /health/ready — readiness",
                "GET /health — combined detailed checks (ReadAuthority)",
                "GET /openapi/v1.json — OpenAPI document"
            },
            documentation = new[]
            {
                SupportBundleDocLinks.PilotRescuePlaybookRelativePath + " — symptom-first pilot triage",
                "docs/TROUBLESHOOTING.md",
                "docs/OPERATOR_QUICKSTART.md",
                "docs/library/OPERATOR_ATLAS.md",
                "docs/PENDING_QUESTIONS.md item 37 (Resolved 2026-05-03 — manual review before external forward; ExecuteAuthority holders only for bundling tenant-identifying/contact PII to third parties)."
            },
            correlation = "Match X-Correlation-ID response header / problem JSON correlationId against API logs."
        };
    }

    private static string BuildReadme(string createdUtcIso, string requesterDisplay, string tenantDisplay, SupportBundleNextStepsDocument nextSteps)
    {
        if (nextSteps is null)
            throw new ArgumentNullException(nameof(nextSteps));
        StringBuilder body = new();
        body.AppendLine("ArchLucid support bundle (server-assembled)");
        body.AppendLine("===========================================");
        body.Append("Generated (UTC): ").AppendLine(createdUtcIso);
        body.Append("Requester:       ").AppendLine(requesterDisplay);
        body.Append("Tenant:          ").AppendLine(tenantDisplay);
        body.AppendLine();
        body.AppendLine("Suggested next steps (generated — advisory)");
        body.AppendLine("-------------------------------------------");
        foreach (string line in nextSteps.SummaryLines)
        {
            body.Append(" - ").AppendLine(line);
        }

        body.AppendLine();
        body.AppendLine("Read first (in order)");
        body.AppendLine("---------------------");
        body.Append(" 1. ").Append(ReadmeFileName).AppendLine("              — this file");
        body.Append(" 2. ").Append(SupportBundleLayout.NextStepsFileName).AppendLine(" — same summary as JSON + structured hints");
        body.Append(" 3. ").Append(ManifestFileName).AppendLine("            — bundle metadata + machine-readable read order");
        body.Append(" 4. ").Append(BuildFileName).AppendLine("               — host build identity (assembly version + runtime)");
        body.Append(" 5. ").Append(EnvironmentFileName).AppendLine("         — redacted ARCHLUCID_* / DOTNET_* env vars");
        body.Append(" 6. ").Append(ReferencesFileName).AppendLine("          — API endpoints, doc links, correlation tip");
        body.AppendLine();
        body.AppendLine("Redaction");
        body.AppendLine("---------");
        body.AppendLine("Bearer tokens, X-Api-Key headers, and password-shaped key=value pairs are replaced");
        body.AppendLine("with [REDACTED] before the bundle is written. Environment variables whose names");
        body.AppendLine("look like secrets show (set)/(not set) only.");
        body.AppendLine();
        body.AppendLine("Open ticket / next steps");
        body.AppendLine("------------------------");
        body.Append(SupportBundleNextStepsDocument.AdvisoryDisclaimer);
        body.AppendLine(" Attach this ZIP to a support ticket.");
        body.AppendLine("Pre-forward checklist (Resolved 2026-05-03): review every file for sensitive context;");
        body.AppendLine("bundles already redact bearer tokens, Api-Key headers, password-shaped lines, and");
        body.AppendLine("mask secret-shaped env vars. Include tenant-identifying or contact data with external");
        body.AppendLine("support only when you explicitly intend that disclosure (requires ExecuteAuthority to download).");
        return body.ToString();
    }

    private static string SerializeIndented<T>(T value)
    {
        return JsonSerializer.Serialize(value, JsonWrite);
    }

    private static byte[] WriteZip(IReadOnlyList<SupportBundleZipEntry> entries)
    {
        using MemoryStream ms = new();
        using (ZipArchive archive = new(ms, ZipArchiveMode.Create, true))
        {
            foreach (SupportBundleZipEntry entry in entries)
            {
                ZipArchiveEntry zipEntry = archive.CreateEntry(entry.Name, CompressionLevel.Optimal);
                using Stream writer = zipEntry.Open();
                writer.Write(entry.Content, 0, entry.Content.Length);
            }
        }

        return ms.ToArray();
    }

    private sealed record SupportBundleZipEntry(string Name, byte[] Content);
}