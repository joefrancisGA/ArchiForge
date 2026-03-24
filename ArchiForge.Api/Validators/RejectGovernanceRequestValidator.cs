using ArchiForge.Api.Models;

using FluentValidation;

namespace ArchiForge.Api.Validators;

public sealed class RejectGovernanceRequestValidator : AbstractValidator<RejectGovernanceRequest>
{
    public RejectGovernanceRequestValidator()
    {
        RuleFor(x => x.ReviewedBy)
            .NotEmpty().WithMessage("ReviewedBy is required.")
            .MaximumLength(200).WithMessage("ReviewedBy must not exceed 200 characters.");

        RuleFor(x => x.ReviewComment)
            .MaximumLength(4000).WithMessage("ReviewComment must not exceed 4000 characters.")
            .When(x => x.ReviewComment is not null);
    }
}
