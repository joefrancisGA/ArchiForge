namespace ArchLucid.Decisioning.Models;

/// <summary>Records a finding engine that failed during snapshot generation (partial-success path).</summary>
public sealed class FindingEngineFailure
{
    public required string EngineType { get; init; }

    public required string Category { get; init; }

    public required string ErrorMessage { get; init; }

    public required string ExceptionType { get; init; }

    public long DurationMs { get; init; }

    public DateTime OccurredUtc { get; init; }
}
