using ArchLucid.Application.Pilots;

namespace ArchLucid.Api.Models.Pilots;

public static class PilotInProductScorecardMapper
{
    public static PilotInProductScorecardResponse ToResponse(PilotInProductScorecardResult r) =>
        new()
        {
            TenantId = r.TenantId,
            TotalRunsCommitted = r.TotalRunsCommitted,
            TotalManifestsCreated = r.TotalManifestsCreated,
            TotalFindingsResolved = r.TotalFindingsResolved,
            AverageTimeToManifestMinutes = r.AverageTimeToManifestMinutes,
            TotalAuditEventsGenerated = r.TotalAuditEventsGenerated,
            TotalGovernanceApprovalsCompleted = r.TotalGovernanceApprovalsCompleted,
            FirstCommitUtc = r.FirstCommitUtc,
            DaysSinceFirstCommit = r.DaysSinceFirstCommit,
            Baselines = r.Baselines is null
                ? null
                : new PilotInProductBaselinesResponse
                {
                    BaselineHoursPerReview = r.Baselines.BaselineHoursPerReview,
                    BaselineReviewsPerQuarter = r.Baselines.BaselineReviewsPerQuarter,
                    BaselineArchitectHourlyCost = r.Baselines.BaselineArchitectHourlyCost,
                    UpdatedUtc = r.Baselines.UpdatedUtc
                },
            RoiEstimate = r.RoiEstimate is null
                ? null
                : new PilotInProductRoiEstimateResponse
                {
                    AnnualReviewCostStatusQuoUsd = r.RoiEstimate.AnnualReviewCostStatusQuoUsd,
                    AnnualReviewSavingsFromReviewTimeLeverUsd = r.RoiEstimate.AnnualReviewSavingsFromReviewTimeLeverUsd,
                    ModelReference = r.RoiEstimate.ModelReference,
                    Currency = r.RoiEstimate.Currency
                }
        };
}
