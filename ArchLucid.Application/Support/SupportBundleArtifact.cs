namespace ArchLucid.Application.Support;

/// <summary>
///     Server-side support-bundle artifact returned by <see cref="ISupportBundleAssembler" /> —
///     ready-to-stream bytes plus the metadata the controller needs to set response headers.
/// </summary>
/// <param name="Bytes">Raw ZIP bytes (already redacted).</param>
/// <param name="FileName">Buyer-facing download file name (e.g. <c>archlucid-support-bundle-20260424-101530Z.zip</c>).</param>
/// <param name="ContentType">MIME type — always <c>application/zip</c>.</param>
/// <param name="GeneratedUtc">When the bundle was assembled.</param>
/// <param name="RetentionDiscardAfterUtc">
///     Suggested UTC instant after which operators should delete the bundle from
///     ticket/attachment storage.
/// </param>
public sealed record SupportBundleArtifact(
    byte[] Bytes,
    string FileName,
    string ContentType,
    DateTimeOffset GeneratedUtc,
    DateTimeOffset RetentionDiscardAfterUtc);
