using ArchiForge.Decisioning.Advisory.Workflow;

using FluentValidation;

namespace ArchiForge.Api.Validators;

public sealed class RecommendationActionRequestValidator : AbstractValidator<RecommendationActionRequest>
{
    public RecommendationActionRequestValidator()
    {
        RuleFor(x => x.Action)
            .NotEmpty().WithMessage("Action is required.")
            .MaximumLength(50).WithMessage("Action must not exceed 50 characters.");

        RuleFor(x => x.Comment)
            .MaximumLength(2000).WithMessage("Comment must not exceed 2000 characters.")
            .When(x => x.Comment is not null);

        RuleFor(x => x.Rationale)
            .MaximumLength(2000).WithMessage("Rationale must not exceed 2000 characters.")
            .When(x => x.Rationale is not null);
    }
}
