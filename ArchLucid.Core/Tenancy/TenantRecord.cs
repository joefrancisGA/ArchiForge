namespace ArchLucid.Core.Tenancy;

/// <summary>Row from <c>dbo.Tenants</c>.</summary>
public sealed class TenantRecord
{
    public Guid Id
    {
        get;
        init;
    }

    public string Name
    {
        get;
        init;
    } = string.Empty;

    public string Slug
    {
        get;
        init;
    } = string.Empty;

    public TenantTier Tier
    {
        get;
        init;
    }

    /// <summary>Azure AD / Entra directory tenant id when the row is linked for multi-org auth.</summary>
    public Guid? EntraTenantId
    {
        get;
        init;
    }

    public DateTimeOffset CreatedUtc
    {
        get;
        init;
    }

    public DateTimeOffset? SuspendedUtc
    {
        get;
        init;
    }

    public DateTimeOffset? TrialStartUtc
    {
        get;
        init;
    }

    public DateTimeOffset? TrialExpiresUtc
    {
        get;
        init;
    }

    public int? TrialRunsLimit
    {
        get;
        init;
    }

    public int TrialRunsUsed
    {
        get;
        init;
    }

    public int? TrialSeatsLimit
    {
        get;
        init;
    }

    public int TrialSeatsUsed
    {
        get;
        init;
    }

    /// <summary><see cref="TrialLifecycleStatus" /> or null when the tenant is not on a self-service trial.</summary>
    public string? TrialStatus
    {
        get;
        init;
    }

    public Guid? TrialSampleRunId
    {
        get;
        init;
    }

    /// <summary>When set, the trial pre-seed worker has queued a simulator run for this tenant.</summary>
    public DateTimeOffset? TrialArchitecturePreseedEnqueuedUtc
    {
        get;
        init;
    }

    /// <summary>
    ///     First committed authority run id (32-char hex as <see cref="Guid" />) after trial bootstrap — drives operator
    ///     deep link.
    /// </summary>
    public Guid? TrialWelcomeRunId
    {
        get;
        init;
    }

    /// <summary>
    ///     First time this tenant committed a golden manifest (UTC anchor for all tiers). Used for sponsor-banner
    ///     time-anchoring in the operator UI; surfaced on the wire as <c>firstCommitUtc</c> on
    ///     <c>GET /v1/tenant/trial-status</c>.
    /// </summary>
    public DateTimeOffset? TrialFirstManifestCommittedUtc
    {
        get;
        init;
    }

    /// <summary>Optional: prospect median hours from architecture request to reviewable package (trial signup).</summary>
    public decimal? BaselineReviewCycleHours
    {
        get;
        init;
    }

    /// <summary>Optional short provenance for <see cref="BaselineReviewCycleHours" />.</summary>
    public string? BaselineReviewCycleSource
    {
        get;
        init;
    }

    /// <summary>When <see cref="BaselineReviewCycleHours" /> was captured (signup time).</summary>
    public DateTimeOffset? BaselineReviewCycleCapturedUtc
    {
        get;
        init;
    }

    /// <summary>Deferrable: person-hours of manual prep per design review (baseline settings; optional at signup path).</summary>
    public decimal? BaselineManualPrepHoursPerReview
    {
        get;
        init;
    }

    /// <summary>Deferrable: people involved in each review for ROI team-cost scaling (baseline settings).</summary>
    public int? BaselinePeoplePerReview
    {
        get;
        init;
    }

    /// <summary>When <see cref="BaselineManualPrepHoursPerReview" /> / <see cref="BaselinePeoplePerReview" /> were last set via settings.</summary>
    public DateTimeOffset? BaselineManualPrepCapturedUtc
    {
        get;
        init;
    }

    public string? CompanySize
    {
        get;
        init;
    }

    public int? ArchitectureTeamSize
    {
        get;
        init;
    }

    public string? IndustryVertical
    {
        get;
        init;
    }

    public string? IndustryVerticalOther
    {
        get;
        init;
    }

    /// <summary>When set, caps <see cref="EnterpriseSeatsUsed" /> for SCIM-provisioned active users; <c>null</c> = unlimited.</summary>
    public int? EnterpriseSeatsLimit
    {
        get;
        init;
    }

    /// <summary>Count of <c>Active=true</c> SCIM users for enterprise seat metering.</summary>
    public int EnterpriseSeatsUsed
    {
        get;
        init;
    }
}
