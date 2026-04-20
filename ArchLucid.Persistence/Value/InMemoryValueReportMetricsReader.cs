namespace ArchLucid.Persistence.Value;

/// <summary>In-memory storage mode: no SQL projections — metrics are zero unless tests replace this registration.</summary>
public sealed class InMemoryValueReportMetricsReader : IValueReportMetricsReader
{
    private static readonly ValueReportRawMetrics Empty = new([], 0, 0, 0, 0);

    public Task<ValueReportRawMetrics> ReadAsync(
        Guid tenantId,
        Guid workspaceId,
        Guid projectId,
        DateTimeOffset fromUtcInclusive,
        DateTimeOffset toUtcExclusive,
        CancellationToken cancellationToken)
    {
        _ = tenantId;
        _ = workspaceId;
        _ = projectId;
        _ = fromUtcInclusive;
        _ = toUtcExclusive;
        _ = cancellationToken;

        return Task.FromResult(Empty);
    }
}
