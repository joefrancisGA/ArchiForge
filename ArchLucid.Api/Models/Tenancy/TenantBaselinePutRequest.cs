namespace ArchLucid.Api.Models.Tenancy;

/// <summary>Body for <c>PUT /v1/tenant/baseline</c> — optional fields merge with the existing tenant row.</summary>
public sealed class TenantBaselinePutRequest
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
}
