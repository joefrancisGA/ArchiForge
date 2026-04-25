namespace ArchLucid.Application.Support;

/// <summary>
///     Assembles the in-product support-bundle ZIP downloaded from
///     <c>POST /v1/admin/support-bundle</c>.
/// </summary>
/// <remarks>
///     Mirrors the file-naming conventions of the CLI's <c>ArchLucid.Cli.Support</c>
///     stack (<c>README.txt</c>, <c>manifest.json</c>, <c>environment.json</c>, …) so support
///     engineers see the same shape regardless of whether the bundle came from
///     <c>archlucid support-bundle</c> or the <c>/admin/support</c> page.
///
///     Implementations MUST run every text section through a redaction pipeline before
///     packing it — server-side bundles can contain environment variables and configuration
///     snapshots that include secrets, and they leave the trust boundary as soon as they
///     hit the response stream.
/// </remarks>
public interface ISupportBundleAssembler
{
    /// <summary>Builds the support-bundle ZIP for the given request.</summary>
    Task<SupportBundleArtifact> AssembleAsync(SupportBundleRequest request, CancellationToken cancellationToken = default);
}
