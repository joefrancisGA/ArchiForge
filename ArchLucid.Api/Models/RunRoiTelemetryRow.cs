namespace ArchLucid.Api.Models;

/// <summary>Dapper row mapped from scoped ROI telemetry aggregation over runs.</summary>
public sealed class RunRoiTelemetryRow
{
    public long TotalRuns { get; init; }

    public decimal? TotalHoursSaved { get; init; }

    public double? AverageTimeToCommitMs { get; init; }
}
