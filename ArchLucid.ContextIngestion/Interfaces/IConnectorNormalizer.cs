using ArchLucid.ContextIngestion.Models;

namespace ArchLucid.ContextIngestion.Interfaces;

/// <summary>
///     Maps a typed connector payload to canonical objects (normalize stage).
/// </summary>
public interface IConnectorNormalizer<in TPayload>
    where TPayload : class
{
    Task<NormalizedContextBatch> NormalizeAsync(
        TPayload payload,
        CancellationToken ct);
}
