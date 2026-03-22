using ArchiForge.Decisioning.Alerts.Tuning;
using FluentValidation;

namespace ArchiForge.Api.Validators;

public sealed class ThresholdRecommendationRequestValidator : AbstractValidator<ThresholdRecommendationRequest>
{
    public ThresholdRecommendationRequestValidator()
    {
        RuleFor(x => x.RuleKind).NotEmpty();
        RuleFor(x => x.TunedMetricType).NotEmpty();
        RuleFor(x => x.CandidateThresholds).NotEmpty().WithMessage("At least one candidate threshold is required.");
        RuleFor(x => x.RecentRunCount).GreaterThan(0);
    }
}
