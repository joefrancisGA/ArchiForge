using System.Data;

namespace ArchLucid.Core.Tenancy;

/// <summary>Persistence for <c>dbo.Tenants</c> / <c>dbo.TenantWorkspaces</c>.</summary>
public interface ITenantRepository
{
    Task<TenantRecord?> GetByIdAsync(Guid tenantId, CancellationToken ct);

    Task<TenantRecord?> GetBySlugAsync(string slug, CancellationToken ct);

    /// <summary>Lookup by Entra directory tenant id (<c>tid</c> claim) when linked.</summary>
    Task<TenantRecord?> GetByEntraTenantIdAsync(Guid entraTenantId, CancellationToken ct);

    Task<IReadOnlyList<TenantRecord>> ListAsync(CancellationToken ct);

    Task InsertTenantAsync(
        Guid tenantId,
        string name,
        string slug,
        TenantTier tier,
        Guid? entraTenantId,
        CancellationToken ct,
        int? enterpriseScimSeatsLimit = null);

    Task InsertWorkspaceAsync(
        Guid workspaceId,
        Guid tenantId,
        string name,
        Guid defaultProjectId,
        CancellationToken ct);

    Task SuspendTenantAsync(Guid tenantId, CancellationToken ct);

    /// <summary>Oldest workspace for the tenant (default bootstrap workspace).</summary>
    Task<TenantWorkspaceLink?> GetFirstWorkspaceAsync(Guid tenantId, CancellationToken ct);

    /// <summary>Persists self-service trial metadata after optional demo seed (SaaS signup).</summary>
    Task CommitSelfServiceTrialAsync(
        Guid tenantId,
        DateTimeOffset trialStartUtc,
        DateTimeOffset trialExpiresUtc,
        int runsLimit,
        int seatsLimit,
        Guid sampleRunId,
        decimal? baselineReviewCycleHours,
        string? baselineReviewCycleSource,
        DateTimeOffset? baselineReviewCycleCapturedUtc,
        CancellationToken ct);

    /// <summary>Marks an active self-service trial as converted after billing activation.</summary>
    /// <param name="newCommercialTier">When set, updates <c>dbo.Tenants.Tier</c> alongside conversion.</param>
    Task MarkTrialConvertedAsync(Guid tenantId, TenantTier? newCommercialTier, CancellationToken ct);

    /// <summary>
    ///     When the tenant is on an active trial with a run limit, increments <see cref="TenantRecord.TrialRunsUsed" /> once
    ///     under <see cref="TenantRecord.TrialRunsLimit" /> and before <see cref="TenantRecord.TrialExpiresUtc" />.
    ///     No-op when the tenant row is missing or not on a metered active trial. Must run in the same SQL transaction as
    ///     inserting the authority run row when <paramref name="connection" /> is supplied.
    /// </summary>
    /// <exception cref="TrialLimitExceededException">Trial expired or run allowance exhausted.</exception>
    Task TryIncrementActiveTrialRunAsync(
        Guid tenantId,
        CancellationToken ct,
        IDbConnection? connection = null,
        IDbTransaction? transaction = null);

    /// <summary>
    ///     Reserves one trial seat for <paramref name="principalKey" /> when the tenant is on an active trial with a seat
    ///     limit.
    ///     Idempotent per (<paramref name="tenantId" />, <paramref name="principalKey" />).
    /// </summary>
    /// <exception cref="TrialLimitExceededException">Seat allowance exhausted for a new principal.</exception>
    Task TryClaimTrialSeatAsync(Guid tenantId, string principalKey, CancellationToken ct);

    /// <summary>
    ///     Tenants eligible for automated lifecycle transitions (self-service trial; excludes converted commercial
    ///     tenants).
    /// </summary>
    Task<IReadOnlyList<Guid>> ListTrialLifecycleAutomationTenantIdsAsync(CancellationToken ct);

    /// <summary>
    ///     Atomically inserts <c>dbo.TenantLifecycleTransitions</c> and updates <c>dbo.Tenants.TrialStatus</c> when the
    ///     current
    ///     status matches <paramref name="expectedCurrentStatus" /> (idempotent retry when <c>false</c>).
    /// </summary>
    Task<bool> TryRecordTrialLifecycleTransitionAsync(
        Guid tenantId,
        string expectedCurrentStatus,
        string nextStatus,
        string reason,
        CancellationToken ct);

    /// <summary>
    ///     Sets <c>TrialFirstManifestCommittedUtc</c> once on the tenant's first golden manifest commit (all tiers) and
    ///     returns funnel timing when this invocation performed the transition. Trial-only metrics/audits are layered in
    ///     <see cref="ITrialFunnelCommitHook" /> — this method only mutates the anchor column.
    /// </summary>
    Task<TrialFirstManifestCommitOutcome?> TryMarkFirstManifestCommittedAsync(
        Guid tenantId,
        DateTimeOffset committedUtc,
        CancellationToken ct);

    /// <summary>E2E harness only: sets <see cref="TenantRecord.TrialExpiresUtc" /> (clock tests; never product code).</summary>
    Task E2eHarnessSetTrialExpiresUtcAsync(Guid tenantId, DateTimeOffset expiresUtc, CancellationToken ct);

    /// <summary>Marks a self-service trial tenant for background simulator pre-seed (idempotent).</summary>
    Task EnqueueTrialArchitecturePreseedAsync(Guid tenantId, CancellationToken ct);

    /// <summary>Tenants with trial pre-seed enqueued but not yet completed.</summary>
    Task<IReadOnlyList<Guid>> ListTenantIdsPendingTrialArchitecturePreseedAsync(int take, CancellationToken ct);

    /// <summary>Persists the committed welcome run id after pre-seed completes.</summary>
    Task MarkTrialArchitecturePreseedCompletedAsync(Guid tenantId, Guid welcomeRunId, CancellationToken ct);

    /// <summary>
    ///     Increments <c>EnterpriseSeatsUsed</c> when a SCIM user becomes <c>Active=true</c> and the tenant has a finite
    ///     <c>EnterpriseSeatsLimit</c>.
    /// </summary>
    /// <returns><c>true</c> when the row was incremented; <c>false</c> when at limit.</returns>
    Task<bool> TryIncrementEnterpriseScimSeatAsync(Guid tenantId, CancellationToken ct);

    /// <summary>Decrements <c>EnterpriseSeatsUsed</c> after a SCIM user transitions from active to inactive.</summary>
    Task DecrementEnterpriseScimSeatAsync(Guid tenantId, CancellationToken ct);
}
