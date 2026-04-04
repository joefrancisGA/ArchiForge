namespace ArchiForge.Persistence.Data.Repositories;

/// <summary>Persistence row for <c>dbo.BackgroundJobs</c>.</summary>
public sealed class BackgroundJobRow
{
    public string JobId { get; init; } = string.Empty;

    public string WorkUnitJson { get; init; } = string.Empty;

    public string State { get; init; } = string.Empty;

    public DateTimeOffset CreatedUtc { get; init; }

    public DateTimeOffset? StartedUtc { get; init; }

    public DateTimeOffset? CompletedUtc { get; init; }

    public string? Error { get; init; }

    public string? FileName { get; init; }

    public string? ContentType { get; init; }

    public int RetryCount { get; init; }

    public int MaxRetries { get; init; }

    public string? ResultBlobName { get; init; }
}
