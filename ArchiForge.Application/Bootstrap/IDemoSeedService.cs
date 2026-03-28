namespace ArchiForge.Application.Bootstrap;

/// <summary>
/// Seeds a deterministic **trusted baseline** (49R pass 2 / Corrected 50R) Contoso Retail story into the
/// primary ArchiForge SQL Server store used by <see cref="ArchiForge.Data.Repositories"/> architecture repositories.
/// Safe to call multiple times: existing rows are skipped or left unchanged.
/// </summary>
public interface IDemoSeedService
{
    /// <summary>
    /// Ensures baseline + hardened runs, manifests, tasks/results, decision traces, governance workflow rows,
    /// and an optional export-history row exist. Does not depend on unfinished later-phase features for success.
    /// </summary>
    Task SeedAsync(CancellationToken cancellationToken = default);
}
