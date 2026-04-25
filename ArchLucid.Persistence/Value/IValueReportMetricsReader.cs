namespace ArchLucid.Persistence.Value;

/// <summary>Projection of scope-scoped value metrics; implemented by SQL and in-memory test doubles.</summary>
public interface IValueReportMetricsReader
{
    Task<ValueReportRawMetrics> ReadAsync(
        Guid tenantId,
        Guid workspaceId,
        Guid projectId,
        DateTimeOffset fromUtcInclusive,
        DateTimeOffset toUtcExclusive,
        CancellationToken cancellationToken);
}
