namespace ArchLucid.Application.Marketing;

/// <summary>
///     Supplies the canonical, ordered list of source files that make up the Trust Center
///     evidence pack — minus the auto-generated <c>README.md</c> (the builder synthesises that).
/// </summary>
/// <remarks>
///     The order returned IS the canonical entry order: it feeds the ETag computation
///     AND the ZIP write order, so two providers that return the same files in different
///     orders would produce different ETags. Production registers
///     <see cref="EmbeddedResourceEvidencePackSourceProvider" />; tests can register
///     an in-memory implementation.
/// </remarks>
public interface IEvidencePackSourceProvider
{
    /// <summary>Returns the canonical ordered set of source entries (no README — builder owns that).</summary>
    Task<IReadOnlyList<EvidencePackEntry>> GetEntriesAsync(CancellationToken cancellationToken = default);
}
