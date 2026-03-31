namespace ArchiForge.Api.Models;

public sealed class BatchReplayManifestSuccessEntry
{
    public required string ComparisonRecordId { get; init; }

    public required string ZipEntryPath { get; init; }
}
