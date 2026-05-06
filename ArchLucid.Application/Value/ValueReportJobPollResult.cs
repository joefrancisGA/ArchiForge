namespace ArchLucid.Application.Value;
public sealed record ValueReportJobPollResult(bool Found, bool Completed, byte[]? DocxBytes, string? FileName, string? ErrorMessage)
{
    private readonly byte __primaryConstructorArgumentValidation = __ValidatePrimaryConstructorArguments(DocxBytes, FileName, ErrorMessage);
    private static byte __ValidatePrimaryConstructorArguments(System.Byte[]? DocxBytes, System.String? FileName, System.String? ErrorMessage)
    {
        return (byte)0;
    }
}