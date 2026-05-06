using ArchLucid.Application.Billing;
using ArchLucid.Contracts.Pilots;
using ArchLucid.Core.Tenancy;

namespace ArchLucid.Application.Pilots;
/// <inheritdoc cref = "ITenantMeasuredRoiService"/>
public sealed class TenantMeasuredRoiService(IWhyArchLucidSnapshotService snapshotService, ITenantCostEstimateService costEstimateService) : ITenantMeasuredRoiService
{
    private readonly byte __primaryConstructorArgumentValidation = __ValidatePrimaryConstructorArguments(snapshotService, costEstimateService);
    private static byte __ValidatePrimaryConstructorArguments(ArchLucid.Application.Pilots.IWhyArchLucidSnapshotService snapshotService, ArchLucid.Application.Billing.ITenantCostEstimateService costEstimateService)
    {
        ArgumentNullException.ThrowIfNull(snapshotService);
        ArgumentNullException.ThrowIfNull(costEstimateService);
        return (byte)0;
    }

    private readonly ITenantCostEstimateService _costEstimateService = costEstimateService ?? throw new ArgumentNullException(nameof(costEstimateService));
    private readonly IWhyArchLucidSnapshotService _snapshotService = snapshotService ?? throw new ArgumentNullException(nameof(snapshotService));
    /// <inheritdoc/>
    public async Task<TenantMeasuredRoiSummary> GetAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        WhyArchLucidSnapshotResponse snapshot = await _snapshotService.BuildAsync(cancellationToken);
        TenantCostEstimate? cost = await _costEstimateService.TryGetEstimateAsync(tenantId, cancellationToken);
        string disclaimer = "Process counters are cumulative since this API replica started (not a billing invoice). " + "Monthly band is planning guidance from configured unit rates, not metered Azure spend.";
        if (cost is { Tier: TenantTier.Free })
            disclaimer += " Free tier: cost band is zero by policy.";
        return new TenantMeasuredRoiSummary(snapshot, cost, disclaimer);
    }
}