using Dm = ArchLucid.Decisioning.Models;

namespace ArchLucid.Application.Runs.Finalization;

/// <summary>Outcome of <see cref="IManifestFinalizationService.FinalizeAsync" />.</summary>
/// <param name="PersistedManifest">Null when <paramref name="WasIdempotentReturn" /> is true.</param>
public sealed record ManifestFinalizationResult(
    Guid ManifestId,
    bool WasIdempotentReturn,
    string ManifestVersion,
    Dm.ManifestDocument? PersistedManifest);
