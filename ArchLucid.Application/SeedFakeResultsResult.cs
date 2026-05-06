namespace ArchLucid.Application;
public sealed record SeedFakeResultsResult(bool Success, int ResultCount, string? Error, ApplicationServiceFailureKind? FailureKind = null)
{
    private readonly byte __primaryConstructorArgumentValidation = __ValidatePrimaryConstructorArguments(Error);
    private static byte __ValidatePrimaryConstructorArguments(System.String? Error)
    {
        return (byte)0;
    }
}