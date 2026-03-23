using ArchiForge.Decisioning.Alerts.Tuning;

using FluentValidation;

namespace ArchiForge.Api.Validators;

/// <summary>FluentValidation for <see cref="ThresholdRecommendationRequest"/> (<c>POST …/alert-tuning/recommend-threshold</c>).</summary>
public sealed class ThresholdRecommendationRequestValidator : AbstractValidator<ThresholdRecommendationRequest>
{
    /// <summary>Requires rule kind, tuned metric, at least one candidate threshold, and positive recent run count.</summary>
    public ThresholdRecommendationRequestValidator()
    {
        RuleFor(x => x.RuleKind).NotEmpty();
        RuleFor(x => x.TunedMetricType).NotEmpty();
        RuleFor(x => x.CandidateThresholds).NotEmpty().WithMessage("At least one candidate threshold is required.");
        RuleFor(x => x.RecentRunCount).GreaterThan(0);
    }
}
