namespace ArchLucid.Application.Marketing;

/// <summary>
///     The fully-built Trust Center evidence pack ZIP plus the metadata the controller
///     needs to send caching headers.
/// </summary>
/// <param name="Bytes">Raw ZIP bytes.</param>
/// <param name="ETag">
///     Strong ETag value (already wrapped in double quotes per RFC 9110), derived from the
///     SHA-256 of the included files' content. Stable across rebuilds when source content is unchanged.
/// </param>
/// <param name="ContentType">MIME type — always <c>application/zip</c>.</param>
/// <param name="BuiltAtUtc">When this artifact was assembled (used for diagnostics, NOT for the ETag).</param>
public sealed record EvidencePackArtifact(
    byte[] Bytes,
    string ETag,
    string ContentType,
    DateTimeOffset BuiltAtUtc);
