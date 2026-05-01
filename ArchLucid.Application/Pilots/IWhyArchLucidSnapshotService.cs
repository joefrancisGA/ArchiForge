using ArchLucid.Contracts.Pilots;

namespace ArchLucid.Application.Pilots;

/// <summary>
///     Builds the read-only telemetry snapshot rendered by the operator-shell <c>/why-archlucid</c> proof page.
///     Combines cumulative <c>ArchLucidInstrumentation</c> counters with a default-scope audit row count and
///     the canonical Contoso Retail demo run id.
/// </summary>
public interface IWhyArchLucidSnapshotService
{
    /// <summary>Builds a fresh snapshot. Safe for concurrent callers; never throws on missing data.</summary>
    Task<WhyArchLucidSnapshotResponse> BuildAsync(CancellationToken cancellationToken);
}
