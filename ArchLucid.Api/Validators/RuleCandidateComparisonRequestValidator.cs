using ArchiForge.Decisioning.Alerts.Simulation;

using FluentValidation;

namespace ArchiForge.Api.Validators;

/// <summary>
/// FluentValidation for <see cref="RuleCandidateComparisonRequest"/> (<c>POST …/alert-simulation/compare-candidates</c>).
/// </summary>
public sealed class RuleCandidateComparisonRequestValidator : AbstractValidator<RuleCandidateComparisonRequest>
{
    /// <summary>Validates rule kind and requires both candidate payloads for the selected kind.</summary>
    public RuleCandidateComparisonRequestValidator()
    {
        RuleFor(x => x.RuleKind)
            .NotEmpty()
            .Must(k => k.Equals("Simple", StringComparison.OrdinalIgnoreCase) ||
                       k.Equals("Composite", StringComparison.OrdinalIgnoreCase))
            .WithMessage("RuleKind must be 'Simple' or 'Composite'.");

        When(
            x => x.RuleKind.Equals("Simple", StringComparison.OrdinalIgnoreCase),
            () =>
            {
                RuleFor(x => x.CandidateASimpleRule).NotNull().WithMessage("CandidateASimpleRule is required for Simple.");
                RuleFor(x => x.CandidateBSimpleRule).NotNull().WithMessage("CandidateBSimpleRule is required for Simple.");
            });

        When(
            x => x.RuleKind.Equals("Composite", StringComparison.OrdinalIgnoreCase),
            () =>
            {
                RuleFor(x => x.CandidateACompositeRule).NotNull().WithMessage("CandidateACompositeRule is required for Composite.");
                RuleFor(x => x.CandidateBCompositeRule).NotNull().WithMessage("CandidateBCompositeRule is required for Composite.");
            });
    }
}
