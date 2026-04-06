using System.Diagnostics.CodeAnalysis;

namespace ArchiForge.Api.Models;

[ExcludeFromCodeCoverage(Justification = "API request/response DTO; no business logic.")]
public sealed class BatchReplayManifestSuccessEntry
{
    public required string ComparisonRecordId { get; init; }
    public required string ZipEntryPath { get; init; }
}
