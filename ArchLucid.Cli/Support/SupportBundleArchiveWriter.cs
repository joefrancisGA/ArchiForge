using System.IO.Compression;
using System.Text;

using ArchLucid.Core.Support;

namespace ArchLucid.Cli.Support;

/// <summary>
///     Writes <see cref="SupportBundlePayload" /> to a deterministic directory layout and optionally zips it.
/// </summary>
public static class SupportBundleArchiveWriter
{
    public const string ManifestFileName = "manifest.json";
    public const string ReadmeFileName = "README.txt";
    public const string BuildFileName = "build.json";
    public const string HealthFileName = "health.json";
    public const string ApiContractFileName = "api-contract.json";
    public const string ConfigFileName = "config-summary.json";
    public const string EnvironmentFileName = "environment.json";
    public const string WorkspaceFileName = "workspace.json";
    public const string ReferencesFileName = "references.json";
    public const string LogsFileName = "logs.json";

    /// <summary>
    ///     Writes JSON files into <paramref name="outputDirectory" /> (created if missing). Returns the directory path.
    /// </summary>
    public static string WriteDirectory(SupportBundlePayload payload, string outputDirectory)
    {
        ArgumentNullException.ThrowIfNull(payload);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputDirectory);

        Directory.CreateDirectory(outputDirectory);

        SupportBundleNextStepsDocument nextSteps = BuildNextSteps(payload);

        WriteFile(Path.Combine(outputDirectory, ManifestFileName),
            SupportBundleCollector.SerializeIndented(payload.Manifest));
        string readme = SupportBundleReadme.Build(
            payload.Manifest.CreatedUtc,
            string.IsNullOrWhiteSpace(payload.ConfigSummary.ApiBaseUrlRedacted)
                ? "(unknown)"
                : payload.ConfigSummary.ApiBaseUrlRedacted,
            payload.Manifest.CliWorkingDirectory,
            nextSteps);
        WriteFile(Path.Combine(outputDirectory, ReadmeFileName), readme);
        WriteFile(Path.Combine(outputDirectory, SupportBundleLayout.NextStepsFileName),
            SupportBundleCollector.SerializeIndented(nextSteps));
        WriteFile(Path.Combine(outputDirectory, BuildFileName),
            SupportBundleCollector.SerializeIndented(payload.Build));
        WriteFile(Path.Combine(outputDirectory, HealthFileName),
            SupportBundleCollector.SerializeIndented(payload.Health));
        WriteFile(Path.Combine(outputDirectory, ApiContractFileName),
            SupportBundleCollector.SerializeIndented(payload.ApiContract));
        WriteFile(Path.Combine(outputDirectory, ConfigFileName),
            SupportBundleCollector.SerializeIndented(payload.ConfigSummary));
        WriteFile(Path.Combine(outputDirectory, EnvironmentFileName),
            SupportBundleCollector.SerializeIndented(payload.Environment));
        WriteFile(Path.Combine(outputDirectory, WorkspaceFileName),
            SupportBundleCollector.SerializeIndented(payload.Workspace));
        WriteFile(Path.Combine(outputDirectory, ReferencesFileName),
            SupportBundleCollector.SerializeIndented(payload.References));
        WriteFile(Path.Combine(outputDirectory, LogsFileName), SupportBundleCollector.SerializeIndented(payload.Logs));

        return outputDirectory;
    }

    /// <summary>
    ///     Applies <see cref="SupportBundleRedactor.RedactSensitivePatterns" /> to every JSON section after
    ///     serialization.
    /// </summary>
    public static string WriteDirectoryWithRedaction(SupportBundlePayload payload, string outputDirectory)
    {
        ArgumentNullException.ThrowIfNull(payload);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputDirectory);

        Directory.CreateDirectory(outputDirectory);

        SupportBundleNextStepsDocument nextSteps = BuildNextSteps(payload);

        WriteRedactedFile(Path.Combine(outputDirectory, ManifestFileName),
            SupportBundleCollector.SerializeIndented(payload.Manifest));
        string readme = SupportBundleReadme.Build(
            payload.Manifest.CreatedUtc,
            string.IsNullOrWhiteSpace(payload.ConfigSummary.ApiBaseUrlRedacted)
                ? "(unknown)"
                : payload.ConfigSummary.ApiBaseUrlRedacted,
            payload.Manifest.CliWorkingDirectory,
            nextSteps);
        WriteRedactedFile(Path.Combine(outputDirectory, ReadmeFileName), readme);
        WriteRedactedFile(Path.Combine(outputDirectory, SupportBundleLayout.NextStepsFileName),
            SupportBundleCollector.SerializeIndented(nextSteps));
        WriteRedactedFile(Path.Combine(outputDirectory, BuildFileName),
            SupportBundleCollector.SerializeIndented(payload.Build));
        WriteRedactedFile(Path.Combine(outputDirectory, HealthFileName),
            SupportBundleCollector.SerializeIndented(payload.Health));
        WriteRedactedFile(Path.Combine(outputDirectory, ApiContractFileName),
            SupportBundleCollector.SerializeIndented(payload.ApiContract));
        WriteRedactedFile(Path.Combine(outputDirectory, ConfigFileName),
            SupportBundleCollector.SerializeIndented(payload.ConfigSummary));
        WriteRedactedFile(Path.Combine(outputDirectory, EnvironmentFileName),
            SupportBundleCollector.SerializeIndented(payload.Environment));
        WriteRedactedFile(Path.Combine(outputDirectory, WorkspaceFileName),
            SupportBundleCollector.SerializeIndented(payload.Workspace));
        WriteRedactedFile(Path.Combine(outputDirectory, ReferencesFileName),
            SupportBundleCollector.SerializeIndented(payload.References));
        WriteRedactedFile(Path.Combine(outputDirectory, LogsFileName),
            SupportBundleCollector.SerializeIndented(payload.Logs));

        return outputDirectory;
    }

    /// <summary>
    ///     Creates <paramref name="zipPath" /> containing the contents of <paramref name="bundleDirectory" /> (flat file list
    ///     at zip root).
    /// </summary>
    public static void WriteZip(string bundleDirectory, string zipPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(bundleDirectory);
        ArgumentException.ThrowIfNullOrWhiteSpace(zipPath);

        if (!Directory.Exists(bundleDirectory))
            throw new DirectoryNotFoundException(bundleDirectory);

        string? parent = Path.GetDirectoryName(Path.GetFullPath(zipPath));

        if (!string.IsNullOrEmpty(parent))

            Directory.CreateDirectory(parent);

        if (File.Exists(zipPath))

            File.Delete(zipPath);

        using FileStream fs = new(zipPath, FileMode.CreateNew, FileAccess.Write, FileShare.None);
        using ZipArchive zip = new(fs, ZipArchiveMode.Create);

        foreach (string filePath in Directory.GetFiles(bundleDirectory, "*", SearchOption.TopDirectoryOnly))
        {
            string name = Path.GetFileName(filePath);
            ZipArchiveEntry entry = zip.CreateEntry(name, CompressionLevel.Optimal);

            using Stream entryStream = entry.Open();
            using FileStream input = File.OpenRead(filePath);
            input.CopyTo(entryStream);
        }
    }

    private static SupportBundleNextStepsDocument BuildNextSteps(SupportBundlePayload payload)
    {
        ArgumentNullException.ThrowIfNull(payload);

        string? excerpt = payload.Logs.LocalLogExcerpt;

        return SupportBundleNextStepsBuilder.BuildForCliClient(
            payload.Manifest.CreatedUtc,
            payload.Health.Live.HttpStatus,
            payload.Health.Ready.HttpStatus,
            payload.Health.Combined.HttpStatus,
            payload.ApiContract.MicrosoftOpenApiV1.HttpStatus,
            payload.Build.ApiVersionError,
            payload.Manifest.ArchLucidJsonPresent,
            !string.IsNullOrWhiteSpace(excerpt));
    }

    private static void WriteFile(string path, string utf8Json)
    {
        File.WriteAllText(path, utf8Json, new UTF8Encoding(false));
    }

    private static void WriteRedactedFile(string path, string content)
    {
        WriteFile(path, SupportBundleRedactor.RedactSensitivePatterns(content));
    }
}
