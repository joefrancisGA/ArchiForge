namespace ArchiForge.Api.Models;

public sealed class BatchReplayManifestFailureEntry
{
    public required string ComparisonRecordId { get; init; }

    public required string Reason { get; init; }

    public required string ExceptionType { get; init; }
}
