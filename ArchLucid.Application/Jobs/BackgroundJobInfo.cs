namespace ArchiForge.Application.Jobs;

public sealed record BackgroundJobInfo(
    string JobId,
    BackgroundJobState State,
    DateTimeOffset CreatedUtc,
    DateTimeOffset? StartedUtc,
    DateTimeOffset? CompletedUtc,
    string? Error,
    string? FileName,
    string? ContentType,
    int RetryCount = 0,
    int MaxRetries = 0);
