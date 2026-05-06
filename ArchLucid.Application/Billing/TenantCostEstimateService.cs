using ArchLucid.Core.Tenancy;
using Microsoft.Extensions.Options;

namespace ArchLucid.Application.Billing;
/// <inheritdoc cref = "ITenantCostEstimateService"/>
public sealed class TenantCostEstimateService(ITenantRepository tenantRepository, IOptionsMonitor<BillingUnitRatesOptions> ratesMonitor) : ITenantCostEstimateService
{
    private readonly byte __primaryConstructorArgumentValidation = __ValidatePrimaryConstructorArguments(tenantRepository, ratesMonitor);
    private static byte __ValidatePrimaryConstructorArguments(ArchLucid.Core.Tenancy.ITenantRepository tenantRepository, Microsoft.Extensions.Options.IOptionsMonitor<ArchLucid.Application.Billing.BillingUnitRatesOptions> ratesMonitor)
    {
        ArgumentNullException.ThrowIfNull(tenantRepository);
        ArgumentNullException.ThrowIfNull(ratesMonitor);
        return (byte)0;
    }

    private readonly IOptionsMonitor<BillingUnitRatesOptions> _ratesMonitor = ratesMonitor ?? throw new ArgumentNullException(nameof(ratesMonitor));
    private readonly ITenantRepository _tenantRepository = tenantRepository ?? throw new ArgumentNullException(nameof(tenantRepository));
    /// <inheritdoc/>
    public async System.Threading.Tasks.Task<ArchLucid.Application.Billing.TenantCostEstimate?> TryGetEstimateAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        TenantRecord? tenant = await _tenantRepository.GetByIdAsync(tenantId, cancellationToken);
        if (tenant is null)
            return null;
        BillingUnitRatesOptions rates = _ratesMonitor.CurrentValue;
        List<string> factors = [$"Tenant tier: {tenant.Tier}.", "Band widened to cover optional Azure OpenAI attach for authority runs."];
        (decimal low, decimal high) = tenant.Tier switch
        {
            TenantTier.Standard => (rates.StandardMonthlyUsdLow, rates.StandardMonthlyUsdHigh),
            TenantTier.Enterprise => (rates.EnterpriseMonthlyUsdLow, rates.EnterpriseMonthlyUsdHigh),
            TenantTier.Free => (0, 0),
            _ => (rates.StandardMonthlyUsdLow, rates.StandardMonthlyUsdHigh)};
        if (tenant.Tier is TenantTier.Free)
            factors.Add("Free tier: guidance defaults to zero — activate a commercial plan for a non-zero band.");
        return new TenantCostEstimate(rates.Currency, tenant.Tier, low, high, factors, rates.MethodologyNote);
    }
}