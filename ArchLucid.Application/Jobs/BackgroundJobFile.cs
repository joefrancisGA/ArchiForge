namespace ArchLucid.Application.Jobs;
public sealed record BackgroundJobFile(string FileName, string ContentType, byte[] Bytes)
{
    private readonly byte __primaryConstructorArgumentValidation = __ValidatePrimaryConstructorArguments(FileName, ContentType, Bytes);
    private static byte __ValidatePrimaryConstructorArguments(System.String FileName, System.String ContentType, System.Byte[] Bytes)
    {
        ArgumentNullException.ThrowIfNull(FileName);
        ArgumentNullException.ThrowIfNull(ContentType);
        ArgumentNullException.ThrowIfNull(Bytes);
        return (byte)0;
    }
}