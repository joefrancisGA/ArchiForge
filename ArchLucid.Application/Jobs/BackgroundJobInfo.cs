namespace ArchLucid.Application.Jobs;
public sealed record BackgroundJobInfo(string JobId, BackgroundJobState State, DateTimeOffset CreatedUtc, DateTimeOffset? StartedUtc, DateTimeOffset? CompletedUtc, string? Error, string? FileName, string? ContentType, int RetryCount = 0, int MaxRetries = 0)
{
    private readonly byte __primaryConstructorArgumentValidation = __ValidatePrimaryConstructorArguments(JobId, Error, FileName, ContentType);
    private static byte __ValidatePrimaryConstructorArguments(System.String JobId, System.String? Error, System.String? FileName, System.String? ContentType)
    {
        ArgumentNullException.ThrowIfNull(JobId);
        return (byte)0;
    }
}