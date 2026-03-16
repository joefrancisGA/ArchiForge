namespace ArchiForge.Api.Jobs;

public enum BackgroundJobState
{
    Pending = 0,
    Running = 1,
    Succeeded = 2,
    Failed = 3
}

public sealed record BackgroundJobInfo(
    string JobId,
    BackgroundJobState State,
    DateTimeOffset CreatedUtc,
    DateTimeOffset? StartedUtc,
    DateTimeOffset? CompletedUtc,
    string? Error,
    string? FileName,
    string? ContentType);

public sealed record BackgroundJobFile(
    string FileName,
    string ContentType,
    byte[] Bytes);

