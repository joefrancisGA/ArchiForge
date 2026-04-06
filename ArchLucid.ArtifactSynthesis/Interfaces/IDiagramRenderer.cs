using ArchiForge.ArtifactSynthesis.Models;

namespace ArchiForge.ArtifactSynthesis.Interfaces;

public interface IDiagramRenderer
{
    string Format { get; }

    string Render(DiagramAst ast);
}
