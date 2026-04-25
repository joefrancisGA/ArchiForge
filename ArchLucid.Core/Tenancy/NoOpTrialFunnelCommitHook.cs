namespace ArchLucid.Core.Tenancy;

/// <summary>No-op hook for hosts/tests that do not need SQL-backed trial funnel side effects.</summary>
public sealed class NoOpTrialFunnelCommitHook : ITrialFunnelCommitHook
{
    public static readonly NoOpTrialFunnelCommitHook Instance = new();

    private NoOpTrialFunnelCommitHook()
    {
    }

    public Task OnTrialTenantManifestCommittedAsync(Guid tenantId, DateTimeOffset committedUtc,
        CancellationToken cancellationToken)
    {
        _ = tenantId;
        _ = committedUtc;
        _ = cancellationToken;

        return Task.CompletedTask;
    }
}
