using ArchiForge.Api.Models;
using ArchiForge.Contracts.Governance;

using FluentValidation;

namespace ArchiForge.Api.Validators;

public sealed class CreateGovernanceActivationRequestValidator : AbstractValidator<CreateGovernanceActivationRequest>
{
    private static readonly string[] ValidEnvironments =
        [GovernanceEnvironment.Dev, GovernanceEnvironment.Test, GovernanceEnvironment.Prod];

    public CreateGovernanceActivationRequestValidator()
    {
        RuleFor(x => x.RunId)
            .NotEmpty().WithMessage("RunId is required.")
            .MaximumLength(64).WithMessage("RunId must not exceed 64 characters.");

        RuleFor(x => x.ManifestVersion)
            .NotEmpty().WithMessage("ManifestVersion is required.")
            .MaximumLength(128).WithMessage("ManifestVersion must not exceed 128 characters.");

        RuleFor(x => x.Environment)
            .NotEmpty().WithMessage("Environment is required.")
            .Must(e => ValidEnvironments.Contains(e, StringComparer.OrdinalIgnoreCase))
            .WithMessage("Environment must be one of: dev, test, prod.");
    }
}
