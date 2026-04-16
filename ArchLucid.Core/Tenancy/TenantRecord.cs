namespace ArchLucid.Core.Tenancy;

/// <summary>Row from <c>dbo.Tenants</c>.</summary>
public sealed class TenantRecord
{
    public Guid Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public string Slug { get; init; } = string.Empty;

    public TenantTier Tier { get; init; }

    /// <summary>Azure AD / Entra directory tenant id when the row is linked for multi-org auth.</summary>
    public Guid? EntraTenantId { get; init; }

    public DateTimeOffset CreatedUtc { get; init; }

    public DateTimeOffset? SuspendedUtc { get; init; }

    public DateTimeOffset? TrialStartUtc { get; init; }

    public DateTimeOffset? TrialExpiresUtc { get; init; }

    public int? TrialRunsLimit { get; init; }

    public int TrialRunsUsed { get; init; }

    public int? TrialSeatsLimit { get; init; }

    public int TrialSeatsUsed { get; init; }

    /// <summary><see cref="TrialLifecycleStatus"/> or null when the tenant is not on a self-service trial.</summary>
    public string? TrialStatus { get; init; }

    public Guid? TrialSampleRunId { get; init; }
}
