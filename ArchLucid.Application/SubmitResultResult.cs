namespace ArchLucid.Application;
public sealed record SubmitResultResult(bool Success, string? ResultId, string? Error, ApplicationServiceFailureKind? FailureKind = null)
{
    private readonly byte __primaryConstructorArgumentValidation = __ValidatePrimaryConstructorArguments(ResultId, Error);
    private static byte __ValidatePrimaryConstructorArguments(System.String? ResultId, System.String? Error)
    {
        return (byte)0;
    }
}