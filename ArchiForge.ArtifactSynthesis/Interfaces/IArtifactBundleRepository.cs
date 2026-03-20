using System.Data;
using ArchiForge.ArtifactSynthesis.Models;

namespace ArchiForge.ArtifactSynthesis.Interfaces;

public interface IArtifactBundleRepository
{
    Task SaveAsync(
        ArtifactBundle bundle,
        CancellationToken ct,
        IDbConnection? connection = null,
        IDbTransaction? transaction = null);

    Task<ArtifactBundle?> GetByManifestIdAsync(Guid manifestId, CancellationToken ct);
}
