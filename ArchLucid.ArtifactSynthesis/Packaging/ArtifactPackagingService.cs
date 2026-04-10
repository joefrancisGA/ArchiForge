using System.IO.Compression;
using System.Text;
using System.Text.Json;

using ArchLucid.ArtifactSynthesis.Models;

namespace ArchLucid.ArtifactSynthesis.Packaging;

/// <summary>
/// Packages <see cref="SynthesizedArtifact"/> instances into single-file exports, manifest-scoped ZIP bundles,
/// or full run-export ZIP packages containing the manifest JSON and an optional decision trace.
/// </summary>
/// <remarks>
/// Entry names are sanitized with <see cref="FileNameSanitizer"/> and made unique within each archive.
/// Reserved names (<c>bundle-index.json</c>, <c>package-metadata.json</c>, etc.) are prefixed with
/// <c>artifact-</c> to avoid collisions.
/// </remarks>
public class ArtifactPackagingService(IArtifactContentTypeResolver contentTypeResolver) : IArtifactPackagingService
{
    private static readonly JsonSerializerOptions JsonWriteIndented = new() { WriteIndented = true };

#pragma warning disable IDE0028 // Simplify collection initialization
    private static readonly HashSet<string> BundleReservedEntryNames = new(StringComparer.OrdinalIgnoreCase)
#pragma warning restore IDE0028 // Simplify collection initialization
    {
        "bundle-index.json",
        "package-metadata.json"
    };

#pragma warning disable IDE0028 // Simplify collection initialization
    private static readonly HashSet<string> RunExportReservedEntryNames = new(StringComparer.OrdinalIgnoreCase)
#pragma warning restore IDE0028 // Simplify collection initialization
    {
        "manifest.json",
        "decision-trace.json",
        "README.txt",
        "package-metadata.json"
    };

    public ArtifactFileExport BuildSingleFileExport(SynthesizedArtifact artifact)
    {
        ArgumentNullException.ThrowIfNull(artifact);

        return new ArtifactFileExport
        {
            FileName = FileNameSanitizer.Sanitize(artifact.Name),
            ContentType = contentTypeResolver.Resolve(artifact),
            Content = Encoding.UTF8.GetBytes(artifact.Content)
        };
    }

    public ArtifactPackage BuildBundlePackage(
        Guid manifestId,
        IReadOnlyList<SynthesizedArtifact> artifacts)
    {
        ArgumentNullException.ThrowIfNull(artifacts);

        using MemoryStream memoryStream = new();

        using (ZipArchive archive = new(memoryStream, ZipArchiveMode.Create, leaveOpen: true))
        {
            HashSet<string> usedEntryNames = new(StringComparer.OrdinalIgnoreCase);

            foreach (SynthesizedArtifact artifact in artifacts.OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase))
            {
                string safe = AvoidReservedEntryName(FileNameSanitizer.Sanitize(artifact.Name), BundleReservedEntryNames);
                string entryName = AllocateUniqueEntryName(safe, usedEntryNames);
                WriteTextEntry(archive, entryName, artifact.Content);
            }

            WriteBundleIndex(archive, artifacts);
            WritePackageMetadata(
                archive,
                new
                {
                    CreatedUtc = DateTime.UtcNow,
                    ManifestId = manifestId,
                    ArtifactCount = artifacts.Count
                });
        }

        return new ArtifactPackage
        {
            PackageFileName = $"artifact-bundle-{manifestId:N}.zip",
            Content = memoryStream.ToArray()
        };
    }

    public ArtifactPackage BuildRunExportPackage(
        Guid runId,
        Guid manifestId,
        IReadOnlyList<SynthesizedArtifact> artifacts,
        string manifestJson,
        string? traceJson = null,
        RunExportReadmeContext? readmeContext = null)
    {
        ArgumentNullException.ThrowIfNull(artifacts);
        ArgumentException.ThrowIfNullOrWhiteSpace(manifestJson);

        using MemoryStream memoryStream = new();

        using (ZipArchive archive = new(memoryStream, ZipArchiveMode.Create, leaveOpen: true))
        {
            HashSet<string> usedEntryNames = new(StringComparer.OrdinalIgnoreCase);

            foreach (SynthesizedArtifact artifact in artifacts.OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase))
            {
                string safeName = AvoidReservedEntryName(FileNameSanitizer.Sanitize(artifact.Name), RunExportReservedEntryNames);
                string relative = $"artifacts/{AllocateUniqueEntryName(safeName, usedEntryNames)}";
                WriteTextEntry(archive, relative, artifact.Content);
            }

            WriteTextEntry(archive, "manifest.json", manifestJson);

            if (!string.IsNullOrWhiteSpace(traceJson))
            {
                WriteTextEntry(archive, "decision-trace.json", traceJson);
            }

            StringBuilder readme = new StringBuilder()
                .AppendLine("ArchLucid run export package")
                .AppendLine("===========================")
                .AppendLine()
                .AppendLine($"Run ID: {runId}")
                .AppendLine($"Manifest ID: {manifestId}");

            if (readmeContext is not null)
            {
                if (!string.IsNullOrWhiteSpace(readmeContext.ManifestDisplayName))
                {
                    readme.AppendLine($"Manifest name: {readmeContext.ManifestDisplayName}");
                }

                if (!string.IsNullOrWhiteSpace(readmeContext.RuleSetLabel))
                {
                    readme.AppendLine($"Rule set: {readmeContext.RuleSetLabel}");
                }

                if (!string.IsNullOrWhiteSpace(readmeContext.ManifestHash))
                {
                    readme.AppendLine($"Manifest hash: {readmeContext.ManifestHash}");
                }
            }

            readme.AppendLine($"Artifact file count: {artifacts.Count}");
            readme.AppendLine();
            readme.AppendLine("Contents:");
            readme.AppendLine("  manifest.json          — committed GoldenManifest (JSON)");
            readme.AppendLine("  decision-trace.json    — authority decision trace when the API included one");
            readme.AppendLine("  artifacts/             — synthesized artifact files (UTF-8 text)");
            readme.AppendLine("  package-metadata.json  — export metadata (UTC timestamp, ids, counts)");
            readme.AppendLine("  README.txt             — this file");
            readme.AppendLine();
            readme.AppendLine("Regenerate Word packages or consulting reports from the API or operator shell when needed.");
            WriteTextEntry(archive, "README.txt", readme.ToString());

            WritePackageMetadata(
                archive,
                new
                {
                    CreatedUtc = DateTime.UtcNow,
                    RunId = runId,
                    ManifestId = manifestId,
                    ArtifactCount = artifacts.Count
                });
        }

        return new ArtifactPackage
        {
            PackageFileName = $"archlucid-run-export-{runId:N}.zip",
            Content = memoryStream.ToArray()
        };
    }

    private static void WriteTextEntry(ZipArchive archive, string entryName, string content)
    {
        ZipArchiveEntry entry = archive.CreateEntry(entryName.Replace('\\', '/'), CompressionLevel.Fastest);
        using Stream entryStream = entry.Open();
        using StreamWriter writer = new(entryStream, Encoding.UTF8);
        writer.Write(content);
    }

    private static void WriteBundleIndex(ZipArchive archive, IReadOnlyList<SynthesizedArtifact> artifacts)
    {
        string indexJson = JsonSerializer.Serialize(
            artifacts.Select(x => new
            {
                x.ArtifactId,
                x.ArtifactType,
                x.Name,
                x.Format,
                x.CreatedUtc,
                x.ContentHash
            }),
            JsonWriteIndented);

        WriteTextEntry(archive, "bundle-index.json", indexJson);
    }

    private static void WritePackageMetadata(ZipArchive archive, object payload)
    {
        string metadataJson = JsonSerializer.Serialize(payload, JsonWriteIndented);
        WriteTextEntry(archive, "package-metadata.json", metadataJson);
    }

    /// <summary>Reserves a unique name within the current archive (flat or prefixed paths).</summary>
    private static string AvoidReservedEntryName(string sanitizedFileName, HashSet<string> reserved)
    {
        return reserved.Contains(sanitizedFileName) ? $"artifact-{sanitizedFileName}" : sanitizedFileName;
    }

    private static string AllocateUniqueEntryName(string sanitizedFileName, HashSet<string> usedEntryNames)
    {
        string candidate = sanitizedFileName;
        int n = 1;
        while (!usedEntryNames.Add(candidate))
        {
            string stem = Path.GetFileNameWithoutExtension(sanitizedFileName);
            string ext = Path.GetExtension(sanitizedFileName);
            candidate = $"{stem}_{n++}{ext}";
        }

        return candidate;
    }
}
