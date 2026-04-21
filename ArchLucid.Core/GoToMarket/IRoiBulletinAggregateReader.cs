namespace ArchLucid.Core.GoToMarket;

/// <summary>Reads anonymized aggregate statistics for quarterly ROI bulletin drafts (admin-only).</summary>
public interface IRoiBulletinAggregateReader
{
    /// <summary>
    /// Returns tenant-supplied baseline aggregates for tenants whose baseline was captured in <paramref name="window"/>.
    /// <see cref="RoiBulletinAggregateReadResult.IsSufficientSample"/> is false when <c>TenantCount &lt; minimumTenantsRequired</c>.
    /// </summary>
    Task<RoiBulletinAggregateReadResult> ReadAsync(RoiBulletinQuarterWindow window, int minimumTenantsRequired, CancellationToken cancellationToken = default);
}
