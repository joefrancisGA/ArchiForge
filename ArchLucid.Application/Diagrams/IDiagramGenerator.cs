using ArchLucid.Contracts.Manifest;

namespace ArchLucid.Application.Diagrams;

/// <summary>
///     Generates a Mermaid diagram string from a <see cref="GoldenManifest" /> using fixed, opinionated defaults.
///     For configurable rendering use <see cref="IManifestDiagramService" /> instead.
/// </summary>
public interface IDiagramGenerator
{
    /// <summary>
    ///     Returns a Mermaid flowchart string representing the services, datastores, and relationships in
    ///     <paramref name="manifest" />.
    /// </summary>
    string GenerateMermaid(GoldenManifest manifest);
}
