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
        CancellationToken ct);

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
        CancellationToken ct);

    /// <summary>Marks an active self-service trial as converted after billing activation.</summary>
    /// <param name="newCommercialTier">When set, updates <c>dbo.Tenants.Tier</c> alongside conversion.</param>
    Task MarkTrialConvertedAsync(Guid tenantId, TenantTier? newCommercialTier, CancellationToken ct);

    /// <summary>
    /// When the tenant is on an active trial with a run limit, increments <see cref="TenantRecord.TrialRunsUsed"/> once
    /// under <see cref="TenantRecord.TrialRunsLimit"/> and before <see cref="TenantRecord.TrialExpiresUtc"/>.
    /// No-op when the tenant row is missing or not on a metered active trial. Must run in the same SQL transaction as
    /// inserting the authority run row when <paramref name="connection"/> is supplied.
    /// </summary>
    /// <exception cref="TrialLimitExceededException">Trial expired or run allowance exhausted.</exception>
    Task TryIncrementActiveTrialRunAsync(
        Guid tenantId,
        CancellationToken ct,
        System.Data.IDbConnection? connection = null,
        System.Data.IDbTransaction? transaction = null);

    /// <summary>
    /// Reserves one trial seat for <paramref name="principalKey"/> when the tenant is on an active trial with a seat limit.
    /// Idempotent per (<paramref name="tenantId"/>, <paramref name="principalKey"/>).
    /// </summary>
    /// <exception cref="TrialLimitExceededException">Seat allowance exhausted for a new principal.</exception>
    Task TryClaimTrialSeatAsync(Guid tenantId, string principalKey, CancellationToken ct);

    /// <summary>Tenants eligible for automated lifecycle transitions (self-service trial; excludes converted commercial tenants).</summary>
    Task<IReadOnlyList<Guid>> ListTrialLifecycleAutomationTenantIdsAsync(CancellationToken ct);

    /// <summary>
    /// Atomically inserts <c>dbo.TenantLifecycleTransitions</c> and updates <c>dbo.Tenants.TrialStatus</c> when the current
    /// status matches <paramref name="expectedCurrentStatus"/> (idempotent retry when <c>false</c>).
    /// </summary>
    Task<bool> TryRecordTrialLifecycleTransitionAsync(
        Guid tenantId,
        string expectedCurrentStatus,
        string nextStatus,
        string reason,
        CancellationToken ct);

    /// <summary>
    /// Sets <c>TrialFirstManifestCommittedUtc</c> once for self-service trial tenants and returns funnel timing when this invocation performed the transition.
    /// </summary>
    Task<TrialFirstManifestCommitOutcome?> TryMarkTrialFirstManifestCommittedAsync(
        Guid tenantId,
        DateTimeOffset committedUtc,
        CancellationToken ct);

    /// <summary>E2E harness only: sets <see cref="TenantRecord.TrialExpiresUtc"/> (clock tests; never product code).</summary>
    Task E2eHarnessSetTrialExpiresUtcAsync(Guid tenantId, DateTimeOffset expiresUtc, CancellationToken ct);
}
