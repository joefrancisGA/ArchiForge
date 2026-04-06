using ArchiForge.Api.Models;
using ArchiForge.Contracts.Governance;

using FluentValidation;

namespace ArchiForge.Api.Validators;

public sealed class CreateGovernancePromotionRequestValidator : AbstractValidator<CreateGovernancePromotionRequest>
{
    private static readonly string[] ValidEnvironments =
        [GovernanceEnvironment.Dev, GovernanceEnvironment.Test, GovernanceEnvironment.Prod];

    public CreateGovernancePromotionRequestValidator()
    {
        RuleFor(x => x.RunId)
            .NotEmpty().WithMessage("RunId is required.")
            .MaximumLength(64).WithMessage("RunId must not exceed 64 characters.");

        RuleFor(x => x.ManifestVersion)
            .NotEmpty().WithMessage("ManifestVersion is required.")
            .MaximumLength(128).WithMessage("ManifestVersion must not exceed 128 characters.");

        RuleFor(x => x.SourceEnvironment)
            .NotEmpty().WithMessage("SourceEnvironment is required.")
            .Must(e => ValidEnvironments.Contains(e, StringComparer.OrdinalIgnoreCase))
            .WithMessage("SourceEnvironment must be one of: dev, test, prod.");

        RuleFor(x => x.TargetEnvironment)
            .NotEmpty().WithMessage("TargetEnvironment is required.")
            .Must(e => ValidEnvironments.Contains(e, StringComparer.OrdinalIgnoreCase))
            .WithMessage("TargetEnvironment must be one of: dev, test, prod.");

        RuleFor(x => x)
            .Must(x => !string.Equals(x.SourceEnvironment, x.TargetEnvironment, StringComparison.OrdinalIgnoreCase))
            .WithMessage("SourceEnvironment and TargetEnvironment must be different.")
            .When(x => !string.IsNullOrEmpty(x.SourceEnvironment) && !string.IsNullOrEmpty(x.TargetEnvironment));

        RuleFor(x => x.PromotedBy)
            .NotEmpty().WithMessage("PromotedBy is required.")
            .MaximumLength(200).WithMessage("PromotedBy must not exceed 200 characters.");

        RuleFor(x => x.Notes)
            .MaximumLength(4000).WithMessage("Notes must not exceed 4000 characters.")
            .When(x => x.Notes is not null);
    }
}
