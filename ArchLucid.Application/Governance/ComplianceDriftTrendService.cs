using ArchLucid.Contracts.Governance;
using ArchLucid.Decisioning.Governance.PolicyPacks;

namespace ArchLucid.Application.Governance;

/// <inheritdoc />
public sealed class ComplianceDriftTrendService(IPolicyPackChangeLogRepository changeLogRepository)
    : IComplianceDriftTrendService
{
    private readonly IPolicyPackChangeLogRepository _changeLogRepository =
        changeLogRepository ?? throw new ArgumentNullException(nameof(changeLogRepository));

    /// <inheritdoc />
    public async Task<IReadOnlyList<ComplianceDriftTrendPoint>> GetTrendAsync(
        Guid tenantId,
        DateTime fromUtc,
        DateTime toUtc,
        TimeSpan bucketSize,
        CancellationToken cancellationToken = default)
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException("Tenant id is required.", nameof(tenantId));

        if (fromUtc >= toUtc)
            throw new ArgumentOutOfRangeException(nameof(toUtc), "toUtc must be greater than fromUtc.");

        if (bucketSize <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(bucketSize));

        IReadOnlyList<PolicyPackChangeLogEntry> entries =
            await _changeLogRepository.GetByTenantInRangeAsync(tenantId, fromUtc, toUtc, cancellationToken);

        long bucketTicks = bucketSize.Ticks;
        Dictionary<DateTime, Dictionary<string, int>> buckets = [];

        foreach (PolicyPackChangeLogEntry entry in entries)
        {
            long offsetTicks = entry.ChangedUtc.Ticks - fromUtc.Ticks;

            if (offsetTicks < 0)
                continue;

            long bucketIndex = offsetTicks / bucketTicks;
            DateTime bucketUtc = fromUtc.AddTicks(bucketIndex * bucketTicks);

            if (bucketUtc >= toUtc)
                continue;

            if (!buckets.TryGetValue(bucketUtc, out Dictionary<string, int>? byType))
            {
                byType = new Dictionary<string, int>(StringComparer.Ordinal);
                buckets[bucketUtc] = byType;
            }

            byType.TryGetValue(entry.ChangeType, out int n);
            byType[entry.ChangeType] = n + 1;
        }

        List<ComplianceDriftTrendPoint> points = [];
        for (DateTime bucket = fromUtc; bucket < toUtc; bucket = bucket.Add(bucketSize))
        {
            if (!buckets.TryGetValue(bucket, out Dictionary<string, int>? byType))
            {
                points.Add(
                    new ComplianceDriftTrendPoint
                    {
                        BucketUtc = bucket,
                        ChangeCount = 0,
                        ChangesByType = new Dictionary<string, int>(StringComparer.Ordinal)
                    });

                continue;
            }

            int total = byType.Values.Sum();
            IReadOnlyDictionary<string, int> frozen =
                new Dictionary<string, int>(byType, StringComparer.Ordinal);

            points.Add(
                new ComplianceDriftTrendPoint { BucketUtc = bucket, ChangeCount = total, ChangesByType = frozen });
        }

        return points;
    }
}
