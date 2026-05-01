using ArchLucid.Core.Diagrams;

namespace ArchLucid.Application.Diagrams;

/// <summary>
///     No-op implementation of <see cref="IDiagramImageRenderer" /> that always returns <c>null</c>.
///     Register this in environments where the Mermaid CLI (<c>mmdc</c>) is not available,
///     so diagram generation degrades gracefully instead of failing.
/// </summary>
public sealed class NullDiagramImageRenderer : IDiagramImageRenderer
{
    /// <inheritdoc />
    public Task<byte[]?> RenderMermaidPngAsync(
        string mermaidDiagram,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult<byte[]?>(null);
    }
}
