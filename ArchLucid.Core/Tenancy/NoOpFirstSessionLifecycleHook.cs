namespace ArchLucid.Core.Tenancy;

/// <summary>No-op implementation for tests that do not model <c>dbo.TenantOnboardingState</c>.</summary>
public sealed class NoOpFirstSessionLifecycleHook : IFirstSessionLifecycleHook
{
    public static readonly NoOpFirstSessionLifecycleHook Instance = new();

    private NoOpFirstSessionLifecycleHook()
    {
    }

    /// <inheritdoc />
    public Task OnSuccessfulManifestCommitAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
