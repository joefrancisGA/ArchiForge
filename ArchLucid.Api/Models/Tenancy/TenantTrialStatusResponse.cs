namespace ArchLucid.Api.Models.Tenancy;

/// <summary>JSON for <c>GET /v1/tenant/trial-status</c>.</summary>
public sealed class TenantTrialStatusResponse
{
    public string Status
    {
        get;
        init;
    } = "None";

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

    public int? DaysRemaining
    {
        get;
        init;
    }

    public int TrialRunsUsed
    {
        get;
        init;
    }

    public int? TrialRunsLimit
    {
        get;
        init;
    }

    public int TrialSeatsUsed
    {
        get;
        init;
    }

    public int? TrialSeatsLimit
    {
        get;
        init;
    }

    public Guid? TrialSampleRunId
    {
        get;
        init;
    }

    /// <summary>When set, operator UI may deep-link first visit to <c>/runs/{id}</c> (pre-seeded welcome run).</summary>
    public Guid? TrialWelcomeRunId
    {
        get;
        init;
    }

    /// <summary>
    ///     UTC of the first committed golden manifest for this tenant, when known. Drives the operator UI sponsor banner
    ///     days-since-first-commit badge (<c>dbo.Tenants.TrialFirstManifestCommittedUtc</c>).
    /// </summary>
    public DateTimeOffset? FirstCommitUtc
    {
        get;
        init;
    }

    /// <summary>Tenant-supplied median review-cycle hours at signup, when captured.</summary>
    public decimal? BaselineReviewCycleHours
    {
        get;
        init;
    }

    /// <summary>Optional provenance note for <see cref="BaselineReviewCycleHours" />.</summary>
    public string? BaselineReviewCycleSource
    {
        get;
        init;
    }

    /// <summary>UTC when baseline review-cycle fields were captured at signup.</summary>
    public DateTimeOffset? BaselineReviewCycleCapturedUtc
    {
        get;
        init;
    }

    /// <summary>
    ///     When <c>true</c>, this tenant is converted to paid but <c>dbo.Tenants.EntraTenantId</c> is not yet bound; call
    ///     <c>POST /v1/tenant/link-entra</c>.
    /// </summary>
    public bool IdentityHandoffPending
    {
        get;
        init;
    }
}
