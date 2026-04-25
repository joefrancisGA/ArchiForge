using ArchLucid.ArtifactSynthesis.Models;
using ArchLucid.ArtifactSynthesis.Packaging;

namespace ArchLucid.Persistence.Queries;

/// <summary>
///     Shared projection helper used by both <see cref="DapperArtifactQueryService" /> and
///     <see cref="InMemoryArtifactQueryService" /> to map <see cref="SynthesizedArtifact" />
///     instances to lightweight <see cref="ArtifactDescriptor" /> projections.
///     Centralised here to prevent the two implementations from diverging silently.
/// </summary>
internal static class ArtifactDescriptorMapper
{
    /// <summary>
    ///     Projects a <see cref="SynthesizedArtifact" /> to an <see cref="ArtifactDescriptor" />
    ///     (omits the raw content bytes).
    /// </summary>
    internal static ArtifactDescriptor ToDescriptor(SynthesizedArtifact artifact)
    {
        return new ArtifactDescriptor
        {
            ArtifactId = artifact.ArtifactId,
            ArtifactType = artifact.ArtifactType,
            Name = artifact.Name,
            Format = artifact.Format,
            CreatedUtc = artifact.CreatedUtc,
            ContentHash = artifact.ContentHash
        };
    }

    /// <summary>
    ///     Projects and orders a collection of <see cref="SynthesizedArtifact" /> instances to a
    ///     read-only list of <see cref="ArtifactDescriptor" /> records sorted by name (ordinal ignore case),
    ///     then <see cref="SynthesizedArtifact.ArtifactId" /> for deterministic UI and export ordering.
    /// </summary>
    internal static IReadOnlyList<ArtifactDescriptor> ToDescriptorList(
        IEnumerable<SynthesizedArtifact> artifacts)
    {
        return OrderSynthesizedArtifacts(artifacts).Select(ToDescriptor).ToList();
    }

    /// <summary>
    ///     Stable ordering for bundle exports and any consumer that iterates full <see cref="SynthesizedArtifact" /> rows.
    /// </summary>
    internal static IReadOnlyList<SynthesizedArtifact> OrderSynthesizedArtifacts(
        IEnumerable<SynthesizedArtifact> artifacts)
    {
        return artifacts
            .OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(x => x.ArtifactId)
            .ToList();
    }
}
