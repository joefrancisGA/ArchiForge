namespace ArchLucid.Application.Marketing;
/// <summary>
///     A single file inside the Trust Center evidence pack.
/// </summary>
/// <param name = "ZipName">File name as it should appear inside the ZIP (no directory components).</param>
/// <param name = "Content">Raw bytes of the file.</param>
public sealed record EvidencePackEntry(string ZipName, byte[] Content)
{
    private readonly byte __primaryConstructorArgumentValidation = __ValidatePrimaryConstructorArguments(ZipName, Content);
    private static byte __ValidatePrimaryConstructorArguments(System.String ZipName, System.Byte[] Content)
    {
        ArgumentNullException.ThrowIfNull(ZipName);
        ArgumentNullException.ThrowIfNull(Content);
        return (byte)0;
    }
}