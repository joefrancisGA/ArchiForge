using ArchLucid.Core.Scoping;
using ArchLucid.Core.Tenancy;

namespace ArchLucid.Application.Tenancy;
/// <summary>
///     Server-side trial gate: loads <c>dbo.Tenants</c> trial columns and rejects mutating work when the tenant is on a
///     self-service trial that has expired, exhausted limits, or entered a post-active lifecycle phase.
/// </summary>
public sealed class TrialLimitGate(ITenantRepository tenantRepository, TimeProvider timeProvider)
{
    private readonly byte __primaryConstructorArgumentValidation = __ValidatePrimaryConstructorArguments(tenantRepository, timeProvider);
    private static byte __ValidatePrimaryConstructorArguments(ArchLucid.Core.Tenancy.ITenantRepository tenantRepository, System.TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(tenantRepository);
        ArgumentNullException.ThrowIfNull(timeProvider);
        return (byte)0;
    }

    private readonly ITenantRepository _tenantRepository = tenantRepository ?? throw new ArgumentNullException(nameof(tenantRepository));
    private readonly TimeProvider _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    /// <summary>
    ///     Throws <see cref = "TrialLimitExceededException"/> when the tenant must not accept mutating authority operations
    ///     (non-DELETE verbs).
    /// </summary>
    public async Task GuardWriteAsync(ScopeContext scope, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(scope);
        if (scope.TenantId == Guid.Empty)
            return;
        TenantRecord? tenant = await _tenantRepository.GetByIdAsync(scope.TenantId, cancellationToken);
        if (tenant is null)
            return;
        if (string.IsNullOrWhiteSpace(tenant.TrialStatus) || string.Equals(tenant.TrialStatus, TrialLifecycleStatus.Converted, StringComparison.Ordinal))
            return;
        DateTimeOffset now = _timeProvider.GetUtcNow();
        if (string.Equals(tenant.TrialStatus, TrialLifecycleStatus.Deleted, StringComparison.Ordinal))
            throw new TrialLimitExceededException(TrialLimitReason.LifecycleWritesFrozen, 0);
        if (IsPostActiveLifecycleWriteFrozen(tenant.TrialStatus))
        {
            int daysRemaining = ComputeDaysRemaining(tenant.TrialExpiresUtc, now);
            throw new TrialLimitExceededException(TrialLimitReason.LifecycleWritesFrozen, daysRemaining);
        }

        if (!string.Equals(tenant.TrialStatus, TrialLifecycleStatus.Active, StringComparison.Ordinal))
            return;
        int daysRemainingActive = ComputeDaysRemaining(tenant.TrialExpiresUtc, now);
        if (tenant.TrialExpiresUtc is { } exp && exp <= now)
            throw new TrialLimitExceededException(TrialLimitReason.Expired, 0);
        if (tenant.TrialRunsLimit is { } runLimit && tenant.TrialRunsUsed >= runLimit)
            throw new TrialLimitExceededException(TrialLimitReason.RunsExceeded, daysRemainingActive);
        if (tenant.TrialSeatsLimit is { } seatLimit && tenant.TrialSeatsUsed >= seatLimit)
            throw new TrialLimitExceededException(TrialLimitReason.SeatsExceeded, daysRemainingActive);
    }

    /// <summary>Throws when HTTP DELETE must be blocked in ReadOnly / ExportOnly / Deleted lifecycle phases.</summary>
    public async Task GuardDeleteAsync(ScopeContext scope, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(scope);
        if (scope.TenantId == Guid.Empty)
            return;
        TenantRecord? tenant = await _tenantRepository.GetByIdAsync(scope.TenantId, cancellationToken);
        if (tenant is null)
            return;
        if (string.IsNullOrWhiteSpace(tenant.TrialStatus) || string.Equals(tenant.TrialStatus, TrialLifecycleStatus.Converted, StringComparison.Ordinal))
            return;
        DateTimeOffset now = _timeProvider.GetUtcNow();
        if (string.Equals(tenant.TrialStatus, TrialLifecycleStatus.ReadOnly, StringComparison.Ordinal) || string.Equals(tenant.TrialStatus, TrialLifecycleStatus.ExportOnly, StringComparison.Ordinal) || string.Equals(tenant.TrialStatus, TrialLifecycleStatus.Deleted, StringComparison.Ordinal))
        {
            int daysRemaining = ComputeDaysRemaining(tenant.TrialExpiresUtc, now);
            throw new TrialLimitExceededException(TrialLimitReason.LifecycleDeletesFrozen, daysRemaining);
        }
    }

    private static bool IsPostActiveLifecycleWriteFrozen(string trialStatus)
    {
        return string.Equals(trialStatus, TrialLifecycleStatus.Expired, StringComparison.Ordinal) || string.Equals(trialStatus, TrialLifecycleStatus.ReadOnly, StringComparison.Ordinal) || string.Equals(trialStatus, TrialLifecycleStatus.ExportOnly, StringComparison.Ordinal);
    }

    private static int ComputeDaysRemaining(DateTimeOffset? trialExpiresUtc, DateTimeOffset now)
    {
        if (trialExpiresUtc is null)
            return 0;
        double totalDays = (trialExpiresUtc.Value - now).TotalDays;
        int days = (int)Math.Floor(totalDays);
        return days < 0 ? 0 : days;
    }
}