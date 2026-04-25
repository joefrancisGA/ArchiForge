using ArchLucid.Decisioning.Models;

namespace ArchLucid.Decisioning.Interfaces;

public interface IGoldenManifestValidator
{
    void Validate(GoldenManifest manifest);
}
