using System.Diagnostics.CodeAnalysis;

namespace ArchiForge.Api.Models;

[ExcludeFromCodeCoverage(Justification = "API request/response DTO; no business logic.")]
public sealed class BatchReplayManifestFailureEntry
{
    public required string ComparisonRecordId { get; init; }
    public required string Reason { get; init; }
    public required string ExceptionType { get; init; }
}
