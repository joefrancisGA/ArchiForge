namespace ArchLucid.Core.Diagrams;

/// <summary>
///     Renders a Mermaid diagram string to a PNG image.
///     Hosts register a no-op implementation or a Mermaid CLI-backed implementation (see application-layer DI).
/// </summary>
public interface IDiagramImageRenderer
{
    /// <summary>
    ///     Renders <paramref name="mermaidDiagram" /> to a PNG byte array.
    ///     Returns <c>null</c> when rendering is not supported, the input is blank, or the CLI/renderer fails (callers embed
    ///     source text instead).
    /// </summary>
    Task<byte[]?> RenderMermaidPngAsync(
        string mermaidDiagram,
        CancellationToken cancellationToken = default);
}
