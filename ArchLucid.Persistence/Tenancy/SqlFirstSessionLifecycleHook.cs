using ArchLucid.Core.Diagnostics;
using ArchLucid.Core.Tenancy;

namespace ArchLucid.Persistence.Tenancy;

/// <summary>
///     Increments <see cref="ArchLucidInstrumentation.FirstSessionCompletedTotal" /> once per tenant after first
///     commit.
/// </summary>
public sealed class SqlFirstSessionLifecycleHook(ITenantOnboardingStateRepository repository)
    : IFirstSessionLifecycleHook
{
    private readonly ITenantOnboardingStateRepository _repository =
        repository ?? throw new ArgumentNullException(nameof(repository));

    /// <inheritdoc />
    public async Task OnSuccessfulManifestCommitAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        if (tenantId == Guid.Empty)
            return;


        bool first = await _repository.TryMarkFirstSessionCompletedAsync(tenantId, cancellationToken);

        if (!first)
            return;


        ArchLucidInstrumentation.RecordFirstSessionCompleted();
        ArchLucidInstrumentation.RecordOperatorTaskSuccess("first_run_committed");
    }
}
