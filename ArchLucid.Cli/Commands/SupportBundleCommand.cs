using System.Diagnostics.CodeAnalysis;

using ArchLucid.Cli;
using ArchLucid.Cli.Support;

namespace ArchLucid.Cli.Commands;

/// <summary>
/// CLI entry for <c>archlucid support-bundle</c>: writes a reviewable JSON bundle (and optional zip).
/// </summary>
[ExcludeFromCodeCoverage(Justification = "Thin CLI wrapper over SupportBundleCollector and IO; collector logic is unit-tested.")]
internal static class SupportBundleCommand
{
    /// <summary>
    /// Parses <paramref name="args"/> after the command name. Supported: <c>--output &lt;dir&gt;</c>, <c>--zip</c>.
    /// Default output: <c>support-bundle-&lt;yyyyMMdd-HHmmss&gt;Z</c> under the current directory.
    /// </summary>
    public static async Task<int> RunAsync(string[] args, CancellationToken cancellationToken = default)
    {
        string cwd = Directory.GetCurrentDirectory();
        ArchLucidProjectScaffolder.ArchLucidCliConfig? config = TryLoadConfig(cwd);

        bool zip = false;
        string? outputOverride = null;

        for (int i = 0; i < args.Length; i++)
        {
            string a = args[i];

            if (string.Equals(a, "--zip", StringComparison.OrdinalIgnoreCase))
            {
                zip = true;

                continue;
            }

            if (string.Equals(a, "--output", StringComparison.OrdinalIgnoreCase)
                || string.Equals(a, "-o", StringComparison.OrdinalIgnoreCase))
            {
                if (i + 1 >= args.Length)
                {
                    await Console.Error.WriteLineAsync("[ArchLucid CLI] support-bundle: missing path after --output.");

                    return CliExitCode.UsageError;
                }

                outputOverride = args[++i];

                continue;
            }

            if (string.Equals(a, "--help", StringComparison.OrdinalIgnoreCase)
                || string.Equals(a, "-h", StringComparison.OrdinalIgnoreCase))
            {
                PrintUsage();

                return CliExitCode.Success;
            }

            await Console.Error.WriteLineAsync($"[ArchLucid CLI] support-bundle: unknown argument '{a}'.");

            return CliExitCode.UsageError;
        }

        string baseUrl = ArchLucidApiClient.ResolveBaseUrl(config);
        string? urlError = ArchLucidApiClient.GetInvalidApiBaseUrlReason(baseUrl);

        if (urlError is not null)
        {
            await Console.Error.WriteLineAsync("[ArchLucid CLI] " + urlError);

            return CliExitCode.ConfigurationError;
        }

        string folderName = "support-bundle-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss", System.Globalization.CultureInfo.InvariantCulture) + "Z";
        string bundleDir = string.IsNullOrWhiteSpace(outputOverride)
            ? Path.Combine(cwd, folderName)
            : Path.GetFullPath(outputOverride);

        Directory.CreateDirectory(bundleDir);

        ArchLucidApiClient client = new(baseUrl);

        SupportBundlePayload payload = await SupportBundleCollector.CollectAsync(client, cwd, config, cancellationToken);

        string written = SupportBundleArchiveWriter.WriteDirectory(payload, bundleDir);

        Console.WriteLine("ArchLucid support bundle written to:");
        Console.WriteLine(written);

        if (zip)
        {
            string zipPath = written + ".zip";
            SupportBundleArchiveWriter.WriteZip(written, zipPath);
            Console.WriteLine("Zip:");
            Console.WriteLine(zipPath);
        }

        Console.WriteLine();
        Console.WriteLine("Review README.txt and JSON files before sending; they exclude secrets by design.");

        return CliExitCode.Success;
    }

    private static void PrintUsage()
    {
        Console.WriteLine("Usage: archlucid support-bundle [--output <dir>] [--zip]");
        Console.WriteLine("  Writes README.txt (read first), manifest.json (triageReadOrder), build.json, health.json,");
        Console.WriteLine("  api-contract.json (bounded GET /openapi/v1.json), config-summary.json, environment.json,");
        Console.WriteLine("  workspace.json, references.json, logs.json under a new UTC-stamped folder (or --output).");
        Console.WriteLine("  --zip  Also creates <folder>.zip next to the folder.");
    }

    private static ArchLucidProjectScaffolder.ArchLucidCliConfig? TryLoadConfig(string cwd)
    {
        string lucidPath = Path.Combine(cwd, ArchLucidProjectScaffolder.CliManifestFileName);
        string legacyPath = Path.Combine(cwd, "archi" + "forge.json");

        if (!File.Exists(lucidPath) && !File.Exists(legacyPath))
        {
            return null;
        }

        try
        {
            return ArchLucidProjectScaffolder.LoadConfig(cwd);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("[ArchLucid CLI] " + ArchLucidProjectScaffolder.CliManifestFileName + " present but invalid: " + ex.Message);

            return null;
        }
    }
}
