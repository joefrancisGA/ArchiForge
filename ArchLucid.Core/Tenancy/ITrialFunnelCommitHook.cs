namespace ArchLucid.Core.Tenancy;

/// <summary>Invoked after a golden manifest commit succeeds so trial funnel metrics/audits can run once per tenant.</summary>
public interface ITrialFunnelCommitHook
{
    Task OnTrialTenantManifestCommittedAsync(Guid tenantId, DateTimeOffset committedUtc,
        CancellationToken cancellationToken);
}
