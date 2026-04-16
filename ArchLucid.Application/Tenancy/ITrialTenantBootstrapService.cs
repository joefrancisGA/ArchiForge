using ArchLucid.Core.Tenancy;

namespace ArchLucid.Application.Tenancy;

/// <summary>Seeds demo data and trial metadata after <see cref="ITenantProvisioningService"/> self-service registration.</summary>
public interface ITrialTenantBootstrapService
{
    /// <summary>Best-effort: demo seed under tenant scope + trial SQL metadata; failures are logged only.</summary>
    /// <param name="auditActorEmail">Email used for durable audit actor fields.</param>
    Task TryBootstrapAfterSelfRegistrationAsync(
        TenantProvisioningResult result,
        string auditActorEmail,
        CancellationToken cancellationToken);
}
