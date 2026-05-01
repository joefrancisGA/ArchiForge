using ArchLucid.Contracts.Pilots;

namespace ArchLucid.Application.Pilots;

/// <summary>
///     Assembles <see cref="SponsorEvidencePackResponse" /> from snapshot, run detail, findings snapshot, deltas, and
///     governance.
/// </summary>
public interface ISponsorEvidencePackService
{
    Task<SponsorEvidencePackResponse> BuildAsync(CancellationToken cancellationToken);
}
