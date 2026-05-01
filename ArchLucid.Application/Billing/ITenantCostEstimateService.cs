namespace ArchLucid.Application.Billing;

/// <summary>Produces non-authoritative spend guidance for the active tenant.</summary>
public interface ITenantCostEstimateService
{
    /// <summary>Returns <see langword="null" /> when the tenant row is missing.</summary>
    Task<TenantCostEstimate?> TryGetEstimateAsync(Guid tenantId, CancellationToken cancellationToken = default);
}
