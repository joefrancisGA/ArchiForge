using ArchiForge.Contracts.Manifest;

namespace ArchiForge.Application.Diagrams;

/// <summary>
/// Generates a Mermaid flowchart diagram from a <see cref="GoldenManifest"/>, with configurable
/// layout, relationship labels, and component grouping via <see cref="ManifestDiagramOptions"/>.
/// </summary>
public interface IManifestDiagramService
{
    /// <summary>
    /// Returns a Mermaid diagram string for <paramref name="manifest"/>.
    /// Uses <see cref="ManifestDiagramOptions"/> defaults when <paramref name="options"/> is <c>null</c>.
    /// </summary>
    string GenerateMermaid(GoldenManifest manifest, ManifestDiagramOptions? options = null);
}

