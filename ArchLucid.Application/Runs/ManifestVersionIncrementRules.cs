using ArchLucid.Contracts.Metadata;

namespace ArchLucid.Application.Runs;

/// <summary>
/// Pure helpers for manifest version strings used at commit time (extracted for unit/property testing).
/// </summary>
internal static class ManifestVersionIncrementRules
{
    /// <summary>
    /// Parses a <c>vN</c> manifest version string and returns <c>v(N+1)</c>.
    /// </summary>
    internal static string IncrementManifestVersion(string currentVersion)
    {
        if (string.IsNullOrWhiteSpace(currentVersion))
            return "v1";

        if (currentVersion.StartsWith("v", StringComparison.OrdinalIgnoreCase) &&
            int.TryParse(currentVersion[1..], out int versionNumber))
            return $"v{versionNumber + 1}";

        throw new InvalidOperationException(
            $"Cannot increment manifest version '{currentVersion}': expected 'vN' format (e.g. 'v1', 'v2'). " +
            "Verify the CurrentManifestVersion stored in the database has not been corrupted.");
    }

    /// <summary>
    /// Version string used when persisting a new committed manifest for <paramref name="run"/>.
    /// </summary>
    internal static string BuildManifestVersionForCommit(ArchitectureRun run, string runId)
    {
        ArgumentNullException.ThrowIfNull(run);
        ArgumentException.ThrowIfNullOrWhiteSpace(runId);

        if (string.IsNullOrWhiteSpace(run.CurrentManifestVersion))
            return $"v1-{runId}";

        return IncrementManifestVersion(run.CurrentManifestVersion);
    }
}
