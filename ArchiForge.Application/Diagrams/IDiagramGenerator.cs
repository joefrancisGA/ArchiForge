using ArchiForge.Contracts.Manifest;

namespace ArchiForge.Application.Diagrams;

public interface IDiagramGenerator
{
    string GenerateMermaid(GoldenManifest manifest);
}
