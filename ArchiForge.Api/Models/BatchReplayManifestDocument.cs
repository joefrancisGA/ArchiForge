namespace ArchiForge.Api.Models;

/// <summary>Serialized as <c>batch-replay-manifest.json</c> inside batch replay ZIP archives.</summary>
public sealed class BatchReplayManifestDocument
{
    public required string GeneratedUtc { get; init; }

    /// <summary>Comparison record IDs processed, in request order (first occurrence wins for duplicates).</summary>
    public required IReadOnlyList<string> ProcessedComparisonRecordIds { get; init; }

    public required IReadOnlyList<BatchReplayManifestSuccessEntry> Succeeded { get; init; }

    public required IReadOnlyList<BatchReplayManifestFailureEntry> Failed { get; init; }
}
