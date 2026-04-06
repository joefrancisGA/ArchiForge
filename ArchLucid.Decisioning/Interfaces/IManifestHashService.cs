using ArchiForge.Decisioning.Models;

namespace ArchiForge.Decisioning.Interfaces;

/// <summary>
/// Computes a deterministic hash over canonical manifest content (shared by decision engine and authority replay).
/// </summary>
public interface IManifestHashService
{
    string ComputeHash(GoldenManifest manifest);
}
