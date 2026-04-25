namespace ArchLucid.Api.Models.Tenancy;

/// <summary>Current deferrable baseline fields from <c>dbo.Tenants</c> for the scoped tenant.</summary>
public sealed class TenantBaselineGetResponse
{
    public decimal? ManualPrepHoursPerReview
    {
        get;
        init;
    }

    public int? PeoplePerReview
    {
        get;
        init;
    }

    public DateTimeOffset? CapturedUtc
    {
        get;
        init;
    }
}
