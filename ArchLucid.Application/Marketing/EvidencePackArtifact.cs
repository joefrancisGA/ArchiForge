namespace ArchLucid.Application.Marketing;
/// <summary>
///     The fully-built Trust Center evidence pack ZIP plus the metadata the controller
///     needs to send caching headers.
/// </summary>
/// <param name = "Bytes">Raw ZIP bytes.</param>
/// <param name = "ETag">
///     Strong ETag value (already wrapped in double quotes per RFC 9110), derived from the
///     SHA-256 of the included files' content. Stable across rebuilds when source content is unchanged.
/// </param>
/// <param name = "ContentType">MIME type — always <c>application/zip</c>.</param>
/// <param name = "BuiltAtUtc">When this artifact was assembled (used for diagnostics, NOT for the ETag).</param>
public sealed record EvidencePackArtifact(byte[] Bytes, string ETag, string ContentType, DateTimeOffset BuiltAtUtc)
{
    private readonly byte __primaryConstructorArgumentValidation = __ValidatePrimaryConstructorArguments(Bytes, ETag, ContentType);
    private static byte __ValidatePrimaryConstructorArguments(System.Byte[] Bytes, System.String ETag, System.String ContentType)
    {
        ArgumentNullException.ThrowIfNull(Bytes);
        ArgumentNullException.ThrowIfNull(ETag);
        ArgumentNullException.ThrowIfNull(ContentType);
        return (byte)0;
    }
}