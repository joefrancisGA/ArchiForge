namespace ArchLucid.Application.Marketing;

/// <summary>
///     Builds the Trust Center evidence-pack ZIP served by
///     <c>GET /v1/marketing/trust-center/evidence-pack.zip</c>.
/// </summary>
/// <remarks>
///     Implementations MUST be deterministic: the same source files MUST produce
///     the same <see cref="EvidencePackArtifact.ETag" /> and the same
///     <see cref="EvidencePackArtifact.Bytes" /> across invocations. The endpoint
///     relies on this for ETag-based 304 negotiation.
/// </remarks>
public interface IEvidencePackBuilder
{
    /// <summary>Builds (or rebuilds) the evidence-pack ZIP from the registered source provider.</summary>
    Task<EvidencePackArtifact> BuildAsync(CancellationToken cancellationToken = default);
}
