using ArchiForge.Decisioning.Interfaces;
using ArchiForge.Decisioning.Models;

namespace ArchiForge.Decisioning.Services;

public class GoldenManifestValidator : IGoldenManifestValidator
{
    public void Validate(GoldenManifest manifest)
    {
        if (manifest.ManifestId == Guid.Empty)
            throw new InvalidOperationException("ManifestId is required.");

        if (manifest.RunId == Guid.Empty)
            throw new InvalidOperationException("RunId is required.");

        if (string.IsNullOrWhiteSpace(manifest.RuleSetId))
            throw new InvalidOperationException("RuleSetId is required.");

        if (manifest.Requirements is null)
            throw new InvalidOperationException("Requirements section is required.");

        if (manifest.Topology is null)
            throw new InvalidOperationException("Topology section is required.");

        if (manifest.Security is null)
            throw new InvalidOperationException("Security section is required.");

        if (manifest.Compliance is null)
            throw new InvalidOperationException("Compliance section is required.");

        if (manifest.Cost is null)
            throw new InvalidOperationException("Cost section is required.");

        if (manifest.Provenance is null)
            throw new InvalidOperationException("Provenance section is required.");
    }
}

