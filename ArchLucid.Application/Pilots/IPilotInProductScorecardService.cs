namespace ArchLucid.Application.Pilots;

public interface IPilotInProductScorecardService
{
    Task<PilotInProductScorecardResult> GetAsync(CancellationToken cancellationToken);

    Task UpsertBaselinesAsync(
        decimal? baselineHoursPerReview,
        int? baselineReviewsPerQuarter,
        decimal? baselineArchitectHourlyCost,
        CancellationToken cancellationToken);
}
