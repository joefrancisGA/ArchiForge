namespace ArchiForge.Application.Diagrams;

/// <summary>
/// Renders a Mermaid diagram string to a PNG image.
/// Register <see cref="NullDiagramImageRenderer"/> in environments where Mermaid CLI is unavailable,
/// or <see cref="MermaidCliDiagramImageRenderer"/> when the <c>mmdc</c> CLI is installed.
/// </summary>
public interface IDiagramImageRenderer
{
    /// <summary>
    /// Renders <paramref name="mermaidDiagram"/> to a PNG byte array.
    /// Returns <c>null</c> when rendering is not supported or the input is blank.
    /// </summary>
    Task<byte[]?> RenderMermaidPngAsync(
        string mermaidDiagram,
        CancellationToken cancellationToken = default);
}
