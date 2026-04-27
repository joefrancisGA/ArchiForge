using System.Collections.Concurrent;

namespace ArchLucid.Persistence.Pilots;

public sealed class InMemoryPilotBaselineRepository : IPilotBaselineRepository
{
    private readonly ConcurrentDictionary<Guid, PilotBaselineRecord> _byTenant = new();

    public Task<PilotBaselineRecord?> GetAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        _ = cancellationToken;

        return Task.FromResult(_byTenant.TryGetValue(tenantId, out PilotBaselineRecord? r) ? r : null);
    }

    public Task UpsertAsync(PilotBaselineRecord record, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(record);
        _ = cancellationToken;

        PilotBaselineRecord copy = new()
        {
            TenantId = record.TenantId,
            BaselineHoursPerReview = record.BaselineHoursPerReview,
            BaselineReviewsPerQuarter = record.BaselineReviewsPerQuarter,
            BaselineArchitectHourlyCost = record.BaselineArchitectHourlyCost,
            UpdatedUtc = record.UpdatedUtc
        };

        _byTenant[record.TenantId] = copy;

        return Task.CompletedTask;
    }
}
