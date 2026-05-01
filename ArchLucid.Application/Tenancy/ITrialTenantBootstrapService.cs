using ArchLucid.Core.Tenancy;

namespace ArchLucid.Application.Tenancy;

/// <summary>Seeds demo data and trial metadata after <see cref="ITenantProvisioningService" /> self-service registration.</summary>
public interface ITrialTenantBootstrapService
{
    /// <summary>Best-effort: demo seed under tenant scope + trial SQL metadata; failures are logged only.</summary>
    /// <param name="auditActorEmail">Email used for durable audit actor fields.</param>
    /// <param name="baselineReviewCycle">When non-null, persisted on <c>dbo.Tenants</c> with the trial commit.</param>
    /// <param name="companyProfile">When non-null, company-size / team / industry fields are persisted with the trial commit.</param>
    Task TryBootstrapAfterSelfRegistrationAsync(
        TenantProvisioningResult result,
        string auditActorEmail,
        TrialSignupBaselineReviewCycleCapture? baselineReviewCycle,
        TrialSignupCompanyProfileCapture? companyProfile,
        CancellationToken cancellationToken);
}
