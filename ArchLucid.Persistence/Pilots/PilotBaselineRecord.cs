namespace ArchLucid.Persistence.Pilots;

public sealed class PilotBaselineRecord
{
    public required Guid TenantId
    {
        get;
        init;
    }

    public decimal? BaselineHoursPerReview
    {
        get;
        init;
    }

    public int? BaselineReviewsPerQuarter
    {
        get;
        init;
    }

    public decimal? BaselineArchitectHourlyCost
    {
        get;
        init;
    }

    public DateTimeOffset UpdatedUtc
    {
        get;
        init;
    }
}
