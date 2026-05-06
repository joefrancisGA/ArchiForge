using ArchLucid.Core.Scoping;
using ArchLucid.Core.Tenancy;

namespace ArchLucid.Application.Tenancy;
/// <summary>
///     Reserves a trial seat for the authenticated principal on first use per tenant (idempotent per tenant + principal
///     key).
/// </summary>
public sealed class TrialSeatAccountant(ITenantRepository tenantRepository)
{
    private readonly byte __primaryConstructorArgumentValidation = __ValidatePrimaryConstructorArguments(tenantRepository);
    private static byte __ValidatePrimaryConstructorArguments(ArchLucid.Core.Tenancy.ITenantRepository tenantRepository)
    {
        ArgumentNullException.ThrowIfNull(tenantRepository);
        return (byte)0;
    }

    private readonly ITenantRepository _tenantRepository = tenantRepository ?? throw new ArgumentNullException(nameof(tenantRepository));
    /// <summary>
    ///     Attempts to claim a seat for <paramref name = "principalKey"/> when the tenant is on a metered active trial.
    /// </summary>
    public Task TryReserveSeatAsync(ScopeContext scope, string principalKey, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(principalKey);
        ArgumentNullException.ThrowIfNull(scope);
        if (scope.TenantId == Guid.Empty)
            return Task.CompletedTask;
        return string.IsNullOrWhiteSpace(principalKey) ? Task.CompletedTask : _tenantRepository.TryClaimTrialSeatAsync(scope.TenantId, principalKey, cancellationToken);
    }
}