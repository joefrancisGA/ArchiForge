using ArchiForge.Decisioning.Models;

namespace ArchiForge.Decisioning.Interfaces;

public interface IGoldenManifestValidator
{
    void Validate(GoldenManifest manifest);
}

