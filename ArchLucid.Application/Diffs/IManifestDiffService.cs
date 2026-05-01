using ArchLucid.Contracts.Manifest;

namespace ArchLucid.Application.Diffs;

/// <summary>
///     Computes a structural diff between two <see cref="GoldenManifest" /> instances,
///     covering services, topology, security controls, governance, and metadata.
/// </summary>
public interface IManifestDiffService
{
    /// <summary>
    ///     Compares <paramref name="left" /> and <paramref name="right" /> and returns a
    ///     <see cref="ManifestDiffResult" /> describing all detected differences.
    /// </summary>
    /// <param name="left">The baseline manifest.</param>
    /// <param name="right">The manifest to compare against the baseline.</param>
    /// <returns>A diff result; never <see langword="null" />.</returns>
    ManifestDiffResult Compare(
        GoldenManifest left,
        GoldenManifest right);
}
