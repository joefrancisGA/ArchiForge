using ArchLucid.Core.GoToMarket;

namespace ArchLucid.Host.Composition.GoToMarket;

/// <summary>In-memory storage: no SQL aggregate surface — bulletin preview always reports insufficient sample.</summary>
internal sealed class InMemoryRoiBulletinAggregateReader : IRoiBulletinAggregateReader
{
    /// <inheritdoc />
    public Task<RoiBulletinAggregateReadResult> ReadAsync(
        RoiBulletinQuarterWindow window,
        int minimumTenantsRequired,
        CancellationToken cancellationToken = default)
    {
        _ = minimumTenantsRequired;

        return Task.FromResult(
            new RoiBulletinAggregateReadResult(
                IsSufficientSample: false,
                TenantCount: 0,
                MeanBaselineHours: null,
                MedianBaselineHours: null,
                P90BaselineHours: null,
                QuarterLabel: window.Label));
    }
}
