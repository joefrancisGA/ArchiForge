namespace ArchLucid.Api.Models.Pilots;

public sealed class PilotScorecardBaselinesPutRequest
{
    public decimal? BaselineHoursPerReview
    {
        get;
        set;
    }

    public int? BaselineReviewsPerQuarter
    {
        get;
        set;
    }

    public decimal? BaselineArchitectHourlyCost
    {
        get;
        set;
    }
}
